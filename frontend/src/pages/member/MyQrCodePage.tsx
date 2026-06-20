import { useEffect, useState } from "react";
import { getApiErrorMessages } from "../../api/apiClient";
import { membersApi } from "../../api/membersApi";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { QrCodeImage } from "../../components/QrCodeImage";
import { MemberDetailsResponse } from "../../types/api";

export const MyQrCodePage = () => {
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
    return <LoadingState label="Loading QR code..." />;
  }

  return (
    <div className="page-stack">
      <PageHeader title="My QR Code" description="Use this QR code and membership code at check-in." />
      <ErrorAlert title="QR code page could not be loaded" messages={errorMessages} />

      {profile ? (
        <section className="panel qr-page">
          <QrCodeImage cacheKey={profile.id} loadImage={() => membersApi.getCurrentMemberQrCode()} />
          <code className="code-inline">{profile.membershipCode}</code>
        </section>
      ) : null}
    </div>
  );
};
