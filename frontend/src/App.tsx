import { Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "./auth/AuthContext";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { LoadingState } from "./components/LoadingState";
import { AdminLayout } from "./layouts/AdminLayout";
import { MemberLayout } from "./layouts/MemberLayout";
import { AccessDeniedPage } from "./pages/AccessDeniedPage";
import { LoginPage } from "./pages/LoginPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { AdminDashboardPage } from "./pages/admin/AdminDashboardPage";
import { CheckInsPage } from "./pages/admin/CheckInsPage";
import { MemberDetailsPage } from "./pages/admin/MemberDetailsPage";
import { MembersPage } from "./pages/admin/MembersPage";
import { PaymentsPage } from "./pages/admin/PaymentsPage";
import { PlansPage } from "./pages/admin/PlansPage";
import { MemberHomePage } from "./pages/member/MemberHomePage";
import { MyCheckInsPage } from "./pages/member/MyCheckInsPage";
import { MyMembershipPage } from "./pages/member/MyMembershipPage";
import { MyPaymentsPage } from "./pages/member/MyPaymentsPage";
import { MyProfilePage } from "./pages/member/MyProfilePage";
import { MyQrCodePage } from "./pages/member/MyQrCodePage";
import { roleHomePath } from "./utils/format";

const RootRedirect = () => {
  const { user, isInitializing } = useAuth();

  if (isInitializing) {
    return <LoadingState fullPage label="Loading application..." />;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={roleHomePath(user.role)} replace />;
};

const LoginRoute = () => {
  const { user, isInitializing } = useAuth();

  if (isInitializing) {
    return <LoadingState fullPage label="Loading application..." />;
  }

  if (user) {
    return <Navigate to={roleHomePath(user.role)} replace />;
  }

  return <LoginPage />;
};

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<RootRedirect />} />
      <Route path="/login" element={<LoginRoute />} />
      <Route path="/access-denied" element={<AccessDeniedPage />} />

      <Route element={<ProtectedRoute roles={["Admin"]} />}>
        <Route path="/admin" element={<AdminLayout />}>
          <Route index element={<AdminDashboardPage />} />
          <Route path="members" element={<MembersPage />} />
          <Route path="members/:id" element={<MemberDetailsPage />} />
          <Route path="plans" element={<PlansPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="check-ins" element={<CheckInsPage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute roles={["Member"]} />}>
        <Route path="/member" element={<MemberLayout />}>
          <Route index element={<MemberHomePage />} />
          <Route path="profile" element={<MyProfilePage />} />
          <Route path="membership" element={<MyMembershipPage />} />
          <Route path="payments" element={<MyPaymentsPage />} />
          <Route path="check-ins" element={<MyCheckInsPage />} />
          <Route path="qr-code" element={<MyQrCodePage />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
