import { useEffect, useState } from "react";
import { getApiErrorMessages } from "../../api/apiClient";
import { membersApi } from "../../api/membersApi";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { MemberDetailsResponse } from "../../types/api";
import { formatDateTime } from "../../utils/format";

export const MyProfilePage = () => {
  const [profile, setProfile] = useState<MemberDetailsResponse | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const loadProfile = async () => {
      try {
        setIsLoading(true);
        setErrorMessages([]);
        const response = await membersApi.getCurrentMember();
        if (isMounted) {
          setProfile(response);
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

    void loadProfile();

    return () => {
      isMounted = false;
    };
  }, []);

  if (isLoading) {
    return <LoadingState label="Loading profile..." />;
  }

  return (
    <div className="page-stack">
      <PageHeader title="My Profile" description="Your member profile details and membership code." />
      <ErrorAlert title="Profile could not be loaded" messages={errorMessages} />

      {profile ? (
        <section className="panel">
          <div className="info-grid">
            <div>
              <span className="info-label">Full name</span>
              <strong>
                {profile.firstName} {profile.lastName}
              </strong>
            </div>
            <div>
              <span className="info-label">Email</span>
              <strong>{profile.email}</strong>
            </div>
            <div>
              <span className="info-label">Phone</span>
              <strong>{profile.phoneNumber || "-"}</strong>
            </div>
            <div>
              <span className="info-label">Membership code</span>
              <strong>
                <code className="code-inline">{profile.membershipCode}</code>
              </strong>
            </div>
            <div>
              <span className="info-label">Created at</span>
              <strong>{formatDateTime(profile.createdAt)}</strong>
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
};
