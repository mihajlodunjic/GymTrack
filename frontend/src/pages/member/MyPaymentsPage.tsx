import { useEffect, useState } from "react";
import { getApiErrorMessages } from "../../api/apiClient";
import { paymentsApi } from "../../api/paymentsApi";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { MembershipPaymentResponse } from "../../types/api";
import { formatCurrency, formatDate, formatDateTime, formatPlanType } from "../../utils/format";

export const MyPaymentsPage = () => {
  const [payments, setPayments] = useState<MembershipPaymentResponse[]>([]);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const loadPayments = async () => {
      try {
        setIsLoading(true);
        setErrorMessages([]);
        const response = await paymentsApi.getMyPayments();
        if (isMounted) {
          setPayments(response);
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

    void loadPayments();

    return () => {
      isMounted = false;
    };
  }, []);

  if (isLoading) {
    return <LoadingState label="Loading payments..." />;
  }

  return (
    <div className="page-stack">
      <PageHeader title="My Payments" description="History of your membership purchases." />
      <ErrorAlert title="Payments could not be loaded" messages={errorMessages} />

      <section className="panel">
        <DataTable
          rows={payments}
          emptyTitle="No payments"
          emptyDescription="Your membership payment history will appear here."
          columns={[
            {
              key: "plan",
              header: "Plan",
              render: (payment) => (
                <div>
                  <strong>{payment.planName}</strong>
                  <div className="table-subtext">{formatPlanType(payment.planType)}</div>
                </div>
              ),
            },
            { key: "amount", header: "Amount", render: (payment) => formatCurrency(payment.amount) },
            { key: "paidAt", header: "Paid at", render: (payment) => formatDateTime(payment.paidAt) },
            { key: "validFrom", header: "Valid from", render: (payment) => formatDate(payment.validFrom) },
            { key: "validUntil", header: "Valid until", render: (payment) => formatDate(payment.validUntil) },
            { key: "remainingVisits", header: "Remaining visits", render: (payment) => payment.remainingVisits ?? "-" },
          ]}
        />
      </section>
    </div>
  );
};
