import { useEffect, useState } from "react";
import { getApiErrorMessages } from "../api/apiClient";
import { ErrorAlert } from "./ErrorAlert";
import { LoadingState } from "./LoadingState";

interface QrCodeImageProps {
  cacheKey: string | number;
  loadImage: () => Promise<Blob>;
}

export const QrCodeImage = ({ cacheKey, loadImage }: QrCodeImageProps) => {
  const [imageUrl, setImageUrl] = useState<string | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;
    let currentObjectUrl: string | null = null;

    const fetchImage = async () => {
      try {
        setIsLoading(true);
        setErrorMessages([]);

        const blob = await loadImage();
        currentObjectUrl = URL.createObjectURL(blob);

        if (isMounted) {
          setImageUrl(currentObjectUrl);
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

    void fetchImage();

    return () => {
      isMounted = false;
      if (currentObjectUrl) {
        URL.revokeObjectURL(currentObjectUrl);
      }
    };
  }, [cacheKey]);

  if (isLoading) {
    return <LoadingState label="Loading QR code..." />;
  }

  if (errorMessages.length > 0) {
    return <ErrorAlert title="QR code could not be loaded" messages={errorMessages} />;
  }

  if (!imageUrl) {
    return null;
  }

  return <img className="qr-image" src={imageUrl} alt="Membership QR code" />;
};
