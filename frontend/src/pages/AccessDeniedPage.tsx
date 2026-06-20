import { Link } from "react-router-dom";

export const AccessDeniedPage = () => (
  <div className="center-screen">
    <div className="message-card">
      <h1>Access denied</h1>
      <p>You do not have permission to access this page.</p>
      <Link className="button button-primary" to="/">
        Back to home
      </Link>
    </div>
  </div>
);
