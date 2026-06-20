import { FormEvent, useEffect, useState } from "react";
import { checkInsApi } from "../../api/checkInsApi";
import { getApiErrorMessages } from "../../api/apiClient";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { StatusBadge } from "../../components/StatusBadge";
import { CheckInResponse } from "../../types/api";
import { formatDateTime } from "../../utils/format";

export const CheckInsPage = () => {
  const [membershipCode, setMembershipCode] = useState("");
  const [note, setNote] = useState("");
  const [result, setResult] = useState<CheckInResponse | null>(null);
  const [checkIns, setCheckIns] = useState<CheckInResponse[]>([]);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadCheckIns = async () => {
    try {
      setIsLoading(true);
      setErrorMessages([]);
      const response = await checkInsApi.getCheckIns();
      setCheckIns(response);
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadCheckIns();
  }, []);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessages([]);
    setResult(null);

    try {
      const response = await checkInsApi.createCheckInByCode(membershipCode.trim().toUpperCase(), { note });
      setResult(response);
      setMembershipCode("");
      setNote("");
      await loadCheckIns();
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="page-stack">
      <PageHeader
        title="Check-ins"
        description="Record a gym entry by membership code and review all check-in history."
      />

      <ErrorAlert title="Check-in action failed" messages={errorMessages} />

      <div className="page-grid">
        <section className="panel panel-side">
          <div className="section-heading">
            <h2>Check in by code</h2>
          </div>
          <form className="form-grid" onSubmit={handleSubmit}>
            <label className="field field-full">
              <span>Membership code</span>
              <input
                placeholder="GYM-2026-0001"
                value={membershipCode}
                onChange={(event) => setMembershipCode(event.target.value)}
                required
              />
            </label>
            <label className="field field-full">
              <span>Note</span>
              <textarea rows={4} value={note} onChange={(event) => setNote(event.target.value)} />
            </label>
            <div className="form-actions field-full">
              <button className="button button-primary" disabled={isSubmitting} type="submit">
                {isSubmitting ? "Submitting..." : "Check in"}
              </button>
            </div>
          </form>

          {result ? (
            <div className="result-card">
              <h3>Latest result</h3>
              <p>
                <strong>{result.memberFullName}</strong>
              </p>
              <p>{result.message}</p>
              <p>Plan: {result.planName}</p>
              <p>Remaining visits: {result.remainingVisits ?? "-"}</p>
              <p>Checked in at: {formatDateTime(result.checkedInAt)}</p>
            </div>
          ) : null}
        </section>

        <section className="panel panel-main">
          {isLoading ? (
            <LoadingState label="Loading check-ins..." />
          ) : (
            <DataTable
              rows={checkIns}
              emptyTitle="No check-ins recorded"
              emptyDescription="Successful and future check-ins will appear here."
              columns={[
                {
                  key: "member",
                  header: "Member",
                  render: (checkIn) => checkIn.memberFullName,
                },
                { key: "plan", header: "Plan", render: (checkIn) => checkIn.planName },
                { key: "checkedInAt", header: "Checked in at", render: (checkIn) => formatDateTime(checkIn.checkedInAt) },
                {
                  key: "validity",
                  header: "Result",
                  render: (checkIn) => (
                    <StatusBadge label={checkIn.wasMembershipValid ? "Accepted" : "Rejected"} tone={checkIn.wasMembershipValid ? "success" : "danger"} />
                  ),
                },
                {
                  key: "remainingVisits",
                  header: "Remaining visits",
                  render: (checkIn) => checkIn.remainingVisits ?? "-",
                },
                { key: "message", header: "Message", render: (checkIn) => checkIn.message },
              ]}
            />
          )}
        </section>
      </div>
    </div>
  );
};
