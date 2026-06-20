import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { checkInsApi } from "../../api/checkInsApi";
import { getApiErrorMessages } from "../../api/apiClient";
import { membersApi } from "../../api/membersApi";
import { paymentsApi } from "../../api/paymentsApi";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { QrCodeImage } from "../../components/QrCodeImage";
import { StatusBadge } from "../../components/StatusBadge";
import {
  CheckInResponse,
  MemberDetailsResponse,
  MembershipPaymentResponse,
  MembershipStatusResponse,
} from "../../types/api";
import { formatCurrency, formatDate, formatDateTime, formatPlanType } from "../../utils/format";

export const MemberDetailsPage = () => {
  const params = useParams();
  const memberId = Number(params.id);
  const [member, setMember] = useState<MemberDetailsResponse | null>(null);
  const [status, setStatus] = useState<MembershipStatusResponse | null>(null);
  const [payments, setPayments] = useState<MembershipPaymentResponse[]>([]);
  const [checkIns, setCheckIns] = useState<CheckInResponse[]>([]);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const loadMemberDetails = async () => {
      if (!Number.isFinite(memberId)) {
        setErrorMessages(["Invalid member id."]);
        setIsLoading(false);
        return;
      }

      try {
        setIsLoading(true);
        setErrorMessages([]);

        const [memberResponse, statusResponse, paymentsResponse, checkInsResponse] = await Promise.all([
          membersApi.getMemberById(memberId),
          membersApi.getMemberStatus(memberId),
          paymentsApi.getPaymentsForMember(memberId),
          checkInsApi.getCheckInsForMember(memberId),
        ]);

        if (isMounted) {
          setMember(memberResponse);
          setStatus(statusResponse);
          setPayments(paymentsResponse);
          setCheckIns(checkInsResponse);
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

    void loadMemberDetails();

    return () => {
      isMounted = false;
    };
  }, [memberId]);

  if (isLoading) {
    return <LoadingState label="Loading member details..." />;
  }

  if (!member) {
    return (
      <div className="page-stack">
        <PageHeader title="Member Details" />
        <ErrorAlert title="Member could not be loaded" messages={errorMessages} />
      </div>
    );
  }

  return (
    <div className="page-stack">
      <PageHeader
        title={`${member.firstName} ${member.lastName}`}
        description="Member profile, membership status, payments, check-ins and QR code."
        actions={
          <Link className="button button-secondary" to="/admin/members">
            Back to members
          </Link>
        }
      />

      <ErrorAlert title="Member details could not be loaded" messages={errorMessages} />

      <div className="detail-grid">
        <section className="panel">
          <div className="section-heading">
            <h2>Profile</h2>
          </div>
          <div className="info-grid">
            <div>
              <span className="info-label">Email</span>
              <strong>{member.email}</strong>
            </div>
            <div>
              <span className="info-label">Phone</span>
              <strong>{member.phoneNumber || "-"}</strong>
            </div>
            <div>
              <span className="info-label">Membership code</span>
              <strong>
                <code className="code-inline">{member.membershipCode}</code>
              </strong>
            </div>
            <div>
              <span className="info-label">Account status</span>
              <StatusBadge label={member.isActive ? "Active" : "Inactive"} tone={member.isActive ? "success" : "danger"} />
            </div>
            <div>
              <span className="info-label">Created</span>
              <strong>{formatDateTime(member.createdAt)}</strong>
            </div>
          </div>
        </section>

        <section className="panel">
          <div className="section-heading">
            <h2>Membership status</h2>
          </div>
          {status ? (
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
                <span className="info-label">Remaining visits</span>
                <strong>{status.remainingVisits ?? "-"}</strong>
              </div>
              <div className="info-block-wide">
                <span className="info-label">Message</span>
                <strong>{status.message}</strong>
              </div>
            </div>
          ) : null}
        </section>

        <section className="panel">
          <div className="section-heading">
            <h2>QR code</h2>
          </div>
          <div className="qr-panel">
            <QrCodeImage cacheKey={member.id} loadImage={() => membersApi.getMemberQrCode(member.id)} />
            <code className="code-inline">{member.membershipCode}</code>
          </div>
        </section>
      </div>

      <section className="panel">
        <div className="section-heading">
          <h2>Membership payments</h2>
        </div>
        <DataTable
          rows={payments}
          emptyTitle="No payments"
          emptyDescription="This member does not have any membership payments yet."
          columns={[
            { key: "plan", header: "Plan", render: (payment) => payment.planName },
            { key: "type", header: "Type", render: (payment) => formatPlanType(payment.planType) },
            { key: "amount", header: "Amount", render: (payment) => formatCurrency(payment.amount) },
            { key: "paidAt", header: "Paid at", render: (payment) => formatDateTime(payment.paidAt) },
            { key: "validFrom", header: "Valid from", render: (payment) => formatDate(payment.validFrom) },
            { key: "validUntil", header: "Valid until", render: (payment) => formatDate(payment.validUntil) },
            { key: "remainingVisits", header: "Remaining visits", render: (payment) => payment.remainingVisits ?? "-" },
          ]}
        />
      </section>

      <section className="panel">
        <div className="section-heading">
          <h2>Check-ins</h2>
        </div>
        <DataTable
          rows={checkIns}
          emptyTitle="No check-ins"
          emptyDescription="This member has no recorded check-ins."
          columns={[
            { key: "checkedInAt", header: "Checked in at", render: (checkIn) => formatDateTime(checkIn.checkedInAt) },
            { key: "planName", header: "Plan", render: (checkIn) => checkIn.planName },
            {
              key: "remainingVisits",
              header: "Remaining visits",
              render: (checkIn) => checkIn.remainingVisits ?? "-",
            },
            { key: "message", header: "Message", render: (checkIn) => checkIn.message },
          ]}
        />
      </section>
    </div>
  );
};
