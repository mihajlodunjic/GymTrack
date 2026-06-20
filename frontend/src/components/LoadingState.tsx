interface LoadingStateProps {
  label?: string;
  fullPage?: boolean;
}

export const LoadingState = ({ label = "Loading...", fullPage = false }: LoadingStateProps) => (
  <div className={fullPage ? "center-screen" : "loading-state"}>{label}</div>
);
