import { createContext, ReactNode, useContext, useEffect, useState } from "react";
import { authApi } from "../api/authApi";
import { clearStoredToken, getStoredToken, setStoredToken, setUnauthorizedHandler } from "../api/apiClient";
import { CurrentUserResponse, LoginRequest } from "../types/api";

interface AuthContextValue {
  user: CurrentUserResponse | null;
  token: string | null;
  isInitializing: boolean;
  isAuthenticated: boolean;
  login: (payload: LoginRequest) => Promise<CurrentUserResponse>;
  logout: () => void;
  refreshCurrentUser: () => Promise<CurrentUserResponse | null>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<CurrentUserResponse | null>(null);
  const [token, setToken] = useState<string | null>(() => getStoredToken());
  const [isInitializing, setIsInitializing] = useState(true);

  const logout = () => {
    clearStoredToken();
    setToken(null);
    setUser(null);
  };

  const refreshCurrentUser = async () => {
    if (!getStoredToken()) {
      setUser(null);
      setToken(null);
      return null;
    }

    const currentUser = await authApi.getCurrentUser();
    setUser(currentUser);
    setToken(getStoredToken());
    return currentUser;
  };

  const login = async (payload: LoginRequest) => {
    const response = await authApi.login(payload);
    setStoredToken(response.token);
    setToken(response.token);
    setUser(response.user);
    return response.user;
  };

  useEffect(() => {
    setUnauthorizedHandler(() => {
      clearStoredToken();
      setToken(null);
      setUser(null);
    });

    return () => {
      setUnauthorizedHandler(null);
    };
  }, []);

  useEffect(() => {
    let isMounted = true;

    const bootstrap = async () => {
      const storedToken = getStoredToken();
      if (!storedToken) {
        if (isMounted) {
          setIsInitializing(false);
        }
        return;
      }

      try {
        const currentUser = await authApi.getCurrentUser();
        if (isMounted) {
          setUser(currentUser);
          setToken(storedToken);
        }
      } catch {
        if (isMounted) {
          logout();
        }
      } finally {
        if (isMounted) {
          setIsInitializing(false);
        }
      }
    };

    void bootstrap();

    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isInitializing,
        isAuthenticated: !!user && !!token,
        login,
        logout,
        refreshCurrentUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
};
