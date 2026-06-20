import { FormEvent, useEffect, useState } from "react";
import { getApiErrorMessages } from "../../api/apiClient";
import { membersApi } from "../../api/membersApi";
import { paymentsApi } from "../../api/paymentsApi";
import { plansApi } from "../../api/plansApi";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { MemberResponse, MembershipPaymentResponse, MembershipPlanResponse } from "../../types/api";
import { formatCurrency, formatDate, formatDateTime, formatPlanType, todayInputValue } from "../../utils/format";

interface PaymentFormValues {
  memberId: string;
  membershipPlanId: string;
  validFrom: string;
  note: string;
}

const emptyPaymentForm: PaymentFormValues = {
  memberId: "",
  membershipPlanId: "",
  validFrom: todayInputValue(),
  note: "",
};

export const PaymentsPage = () => {
  const [payments, setPayments] = useState<MembershipPaymentResponse[]>([]);
  const [members, setMembers] = useState<MemberResponse[]>([]);
  const [plans, setPlans] = useState<MembershipPlanResponse[]>([]);
  const [formValues, setFormValues] = useState<PaymentFormValues>(emptyPaymentForm);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadPageData = async () => {
    try {
      setIsLoading(true);
      setErrorMessages([]);

      const [paymentsResponse, membersResponse, plansResponse] = await Promise.all([
        paymentsApi.getPayments(),
        membersApi.getMembers(),
        plansApi.getPlans(),
      ]);

      setPayments(paymentsResponse);
      setMembers(membersResponse);
      setPlans(plansResponse);

      if (!formValues.memberId && membersResponse.length > 0 && plansResponse.length > 0) {
        setFormValues((current) => ({
          ...current,
          memberId: String(membersResponse[0].id),
          membershipPlanId: String(plansResponse[0].id),
        }));
      }
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadPageData();
  }, []);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessages([]);

    try {
      await paymentsApi.createPayment({
        memberId: Number(formValues.memberId),
        membershipPlanId: Number(formValues.membershipPlanId),
        validFrom: `${formValues.validFrom}T00:00:00`,
        note: formValues.note,
      });

      setFormValues((current) => ({
        ...current,
        validFrom: todayInputValue(),
        note: "",
      }));

      await loadPageData();
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="page-stack">
      <PageHeader
        title="Membership Payments"
        description="Create purchased memberships and review all membership payment records."
      />

      <ErrorAlert title="Payments action failed" messages={errorMessages} />

      <div className="page-grid">
        <section className="panel panel-side">
          <div className="section-heading">
            <h2>Create payment</h2>
          </div>
          <form className="form-grid" onSubmit={handleSubmit}>
            <label className="field field-full">
              <span>Member</span>
              <select
                value={formValues.memberId}
                onChange={(event) =>
                  setFormValues((current) => ({ ...current, memberId: event.target.value }))
                }
                required
              >
                <option value="">Select member</option>
                {members.map((member) => (
                  <option key={member.id} value={member.id}>
                    {member.firstName} {member.lastName} ({member.membershipCode})
                  </option>
                ))}
              </select>
            </label>
            <label className="field field-full">
              <span>Membership plan</span>
              <select
                value={formValues.membershipPlanId}
                onChange={(event) =>
                  setFormValues((current) => ({ ...current, membershipPlanId: event.target.value }))
                }
                required
              >
                <option value="">Select plan</option>
                {plans.map((plan) => (
                  <option key={plan.id} value={plan.id}>
                    {plan.name} ({formatPlanType(plan.planType)})
                  </option>
                ))}
              </select>
            </label>
            <label className="field field-full">
              <span>Valid from</span>
              <input
                type="date"
                value={formValues.validFrom}
                onChange={(event) =>
                  setFormValues((current) => ({ ...current, validFrom: event.target.value }))
                }
                required
              />
            </label>
            <label className="field field-full">
              <span>Note</span>
              <textarea
                rows={4}
                value={formValues.note}
                onChange={(event) => setFormValues((current) => ({ ...current, note: event.target.value }))}
              />
            </label>
            <div className="form-actions field-full">
              <button className="button button-primary" disabled={isSubmitting} type="submit">
                {isSubmitting ? "Saving..." : "Create membership"}
              </button>
            </div>
          </form>
        </section>

        <section className="panel panel-main">
          {isLoading ? (
            <LoadingState label="Loading payments..." />
          ) : (
            <DataTable
              rows={payments}
              emptyTitle="No membership payments"
              emptyDescription="Create the first membership purchase from the form."
              columns={[
                {
                  key: "member",
                  header: "Member",
                  render: (payment) => payment.memberFullName,
                },
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
                { key: "totalVisits", header: "Total visits", render: (payment) => payment.totalVisits ?? "-" },
                { key: "usedVisits", header: "Used visits", render: (payment) => payment.usedVisits ?? "-" },
                {
                  key: "remainingVisits",
                  header: "Remaining visits",
                  render: (payment) => payment.remainingVisits ?? "-",
                },
              ]}
            />
          )}
        </section>
      </div>
    </div>
  );
};
