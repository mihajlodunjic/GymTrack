import { Navigate, Outlet, useLocation } from "react-router-dom";
import { UserRole } from "../types/api";
import { useAuth } from "./AuthContext";
import { LoadingState } from "../components/LoadingState";
import { AccessDeniedPage } from "../pages/AccessDeniedPage";

interface ProtectedRouteProps {
  roles?: UserRole[];
}

export const ProtectedRoute = ({ roles }: ProtectedRouteProps) => {
  const { user, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return <LoadingState fullPage label="Loading application..." />;
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (roles && !roles.includes(user.role)) {
    return <AccessDeniedPage />;
  }

  return <Outlet />;
};
