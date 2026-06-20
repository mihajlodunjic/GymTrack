import { Link } from "react-router-dom";

export const NotFoundPage = () => (
  <div className="center-screen">
    <div className="message-card">
      <h1>Page not found</h1>
      <p>The page you requested does not exist.</p>
      <Link className="button button-primary" to="/">
        Go back
      </Link>
    </div>
  </div>
);
