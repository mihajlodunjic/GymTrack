import { useEffect, useState } from "react";
import { checkInsApi } from "../../api/checkInsApi";
import { getApiErrorMessages } from "../../api/apiClient";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { CheckInResponse } from "../../types/api";
import { formatDateTime } from "../../utils/format";

export const MyCheckInsPage = () => {
  const [checkIns, setCheckIns] = useState<CheckInResponse[]>([]);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const loadCheckIns = async () => {
      try {
        setIsLoading(true);
        setErrorMessages([]);
        const response = await checkInsApi.getMyCheckIns();
        if (isMounted) {
          setCheckIns(response);
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

    void loadCheckIns();

    return () => {
      isMounted = false;
    };
  }, []);

  if (isLoading) {
    return <LoadingState label="Loading check-ins..." />;
  }

  return (
    <div className="page-stack">
      <PageHeader title="My Check-ins" description="History of your gym check-ins." />
      <ErrorAlert title="Check-ins could not be loaded" messages={errorMessages} />

      <section className="panel">
        <DataTable
          rows={checkIns}
          emptyTitle="No check-ins"
          emptyDescription="Your check-in history will appear here."
          columns={[
            { key: "checkedInAt", header: "Checked in at", render: (checkIn) => formatDateTime(checkIn.checkedInAt) },
            { key: "plan", header: "Plan", render: (checkIn) => checkIn.planName },
            { key: "remainingVisits", header: "Remaining visits", render: (checkIn) => checkIn.remainingVisits ?? "-" },
            { key: "message", header: "Message", render: (checkIn) => checkIn.message },
          ]}
        />
      </section>
    </div>
  );
};
