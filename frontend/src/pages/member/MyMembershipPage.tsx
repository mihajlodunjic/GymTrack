import { useEffect, useState } from "react";
import { getApiErrorMessages } from "../../api/apiClient";
import { membersApi } from "../../api/membersApi";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { StatusBadge } from "../../components/StatusBadge";
import { MembershipStatusResponse } from "../../types/api";
import { formatDate, formatPlanType } from "../../utils/format";

export const MyMembershipPage = () => {
  const [status, setStatus] = useState<MembershipStatusResponse | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const loadStatus = async () => {
      try {
        setIsLoading(true);
        setErrorMessages([]);
        const response = await membersApi.getCurrentMemberStatus();
        if (isMounted) {
          setStatus(response);
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessages(getApiErrorMessages(error));
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void loadStatus();

    return () => {
      isMounted = false;
    };
  }, []);

  if (isLoading) {
    return <LoadingState label="Loading membership status..." />;
  }

  return (
    <div className="page-stack">
      <PageHeader title="My Membership" description="Current membership details and active membership message." />
      <ErrorAlert title="Membership status could not be loaded" messages={errorMessages} />

      {status ? (
        <section className="panel">
          <div className="info-grid">
            <div>
              <span className="info-label">Status</span>
              <StatusBadge
                label={status.hasActiveMembership ? "Active membership" : "No active membership"}
                tone={status.hasActiveMembership ? "success" : "warning"}
              />
            </div>
            <div>
              <span className="info-label">Plan</span>
              <strong>{status.planName || "-"}</strong>
            </div>
            <div>
              <span className="info-label">Plan type</span>
              <strong>{formatPlanType(status.planType)}</strong>
            </div>
            <div>
              <span className="info-label">Valid from</span>
              <strong>{formatDate(status.validFrom)}</strong>
            </div>
            <div>
              <span className="info-label">Valid until</span>
              <strong>{formatDate(status.validUntil)}</strong>
            </div>
            <div>
              <span className="info-label">Total visits</span>
              <strong>{status.totalVisits ?? "-"}</strong>
            </div>
            <div>
              <span className="info-label">Used visits</span>
              <strong>{status.usedVisits ?? "-"}</strong>
            </div>
            <div>
              <span className="info-label">Remaining visits</span>
              <strong>{status.remainingVisits ?? "-"}</strong>
            </div>
            <div className="info-block-wide">
              <span className="info-label">Message</span>
              <strong>{status.message}</strong>
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
};
