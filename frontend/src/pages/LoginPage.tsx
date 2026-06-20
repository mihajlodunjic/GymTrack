import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getApiErrorMessages } from "../api/apiClient";
import { useAuth } from "../auth/AuthContext";
import { ErrorAlert } from "../components/ErrorAlert";
import { roleHomePath } from "../utils/format";

export const LoginPage = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [formValues, setFormValues] = useState({
    email: "admin@gymtrack.local",
    password: "Admin123!",
  });
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessages([]);

    try {
      const user = await login(formValues);
      navigate(roleHomePath(user.role), { replace: true });
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="auth-card-header">
          <span className="brand-title">GymTrack</span>
          <h1>Login</h1>
          <p>Use your backend credentials to access the application.</p>
        </div>

        <ErrorAlert title="Login failed" messages={errorMessages} />

        <form className="form-stack" onSubmit={handleSubmit}>
          <label className="field">
            <span>Email</span>
            <input
              autoComplete="username"
              name="email"
              type="email"
              value={formValues.email}
              onChange={(event) =>
                setFormValues((current) => ({ ...current, email: event.target.value }))
              }
              required
            />
          </label>

          <label className="field">
            <span>Password</span>
            <input
              autoComplete="current-password"
              name="password"
              type="password"
              value={formValues.password}
              onChange={(event) =>
                setFormValues((current) => ({ ...current, password: event.target.value }))
              }
              required
            />
          </label>

          <button className="button button-primary button-block" disabled={isSubmitting} type="submit">
            {isSubmitting ? "Signing in..." : "Sign in"}
          </button>
        </form>

        <div className="auth-hint">
          <strong>Development admin:</strong> `admin@gymtrack.local` / `Admin123!`
        </div>
      </div>
    </div>
  );
};
