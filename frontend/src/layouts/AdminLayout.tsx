import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { getApiBaseUrl } from "../api/apiClient";

const navigation = [
  { to: "/admin", label: "Dashboard", end: true },
  { to: "/admin/members", label: "Members" },
  { to: "/admin/plans", label: "Membership Plans" },
  { to: "/admin/payments", label: "Payments" },
  { to: "/admin/check-ins", label: "Check-ins" },
];

export const AdminLayout = () => {
  const { user, logout } = useAuth();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <span className="brand-title">GymTrack</span>
          <span className="brand-subtitle">Admin panel</span>
        </div>
        <nav className="sidebar-nav">
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => (isActive ? "nav-link active" : "nav-link")}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          <div className="user-chip">
            <strong>{user?.email}</strong>
            <span>{getApiBaseUrl()}</span>
          </div>
          <button className="button button-secondary button-block" onClick={logout} type="button">
            Logout
          </button>
        </div>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
};
