import { useEffect, useState } from "react";
import { getApiErrorMessages } from "../../api/apiClient";
import { membersApi } from "../../api/membersApi";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { StatCard } from "../../components/StatCard";
import { MemberDetailsResponse, MembershipStatusResponse } from "../../types/api";
import { formatDate, formatPlanType } from "../../utils/format";

export const MemberHomePage = () => {
  const [profile, setProfile] = useState<MemberDetailsResponse | null>(null);
  const [status, setStatus] = useState<MembershipStatusResponse | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const loadOverview = async () => {
      try {
        setIsLoading(true);
        setErrorMessages([]);

        const [profileResponse, statusResponse] = await Promise.all([
          membersApi.getCurrentMember(),
          membersApi.getCurrentMemberStatus(),
        ]);

        if (isMounted) {
          setProfile(profileResponse);
          setStatus(statusResponse);
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

    void loadOverview();

    return () => {
      isMounted = false;
    };
  }, []);

  if (isLoading) {
    return <LoadingState label="Loading member overview..." />;
  }

  return (
    <div className="page-stack">
      <PageHeader
        title="Member Overview"
        description="Quick overview of your account, membership status and membership code."
      />

      <ErrorAlert title="Overview could not be loaded" messages={errorMessages} />

      <div className="stats-grid">
        <StatCard label="Member" value={profile ? `${profile.firstName} ${profile.lastName}` : "-"} />
        <StatCard label="Membership code" value={profile?.membershipCode || "-"} />
        <StatCard label="Membership status" value={status?.hasActiveMembership ? "Active" : "Inactive"} />
        <StatCard label="Plan" value={status?.planName || "-"} hint={formatPlanType(status?.planType)} />
        <StatCard label="Valid until" value={formatDate(status?.validUntil)} />
        <StatCard label="Remaining visits" value={status?.remainingVisits ?? "-"} />
      </div>

      {status ? (
        <section className="panel">
          <div className="section-heading">
            <h2>Current membership message</h2>
          </div>
          <p>{status.message}</p>
        </section>
      ) : null}
    </div>
  );
};
