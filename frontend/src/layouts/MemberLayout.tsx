import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

const navigation = [
  { to: "/member", label: "Overview", end: true },
  { to: "/member/profile", label: "My Profile" },
  { to: "/member/membership", label: "My Membership" },
  { to: "/member/payments", label: "My Payments" },
  { to: "/member/check-ins", label: "My Check-ins" },
  { to: "/member/qr-code", label: "My QR Code" },
];

export const MemberLayout = () => {
  const { user, logout } = useAuth();

  return (
    <div className="member-shell">
      <header className="member-header">
        <div>
          <span className="brand-title">GymTrack</span>
          <span className="brand-subtitle">Member portal</span>
        </div>
        <div className="member-actions">
          <span className="member-email">{user?.email}</span>
          <button className="button button-secondary" onClick={logout} type="button">
            Logout
          </button>
        </div>
      </header>
      <nav className="member-nav">
        {navigation.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) => (isActive ? "member-nav-link active" : "member-nav-link")}
          >
            {item.label}
          </NavLink>
        ))}
      </nav>
      <main className="member-content">
        <Outlet />
      </main>
    </div>
  );
};
