import { request } from "./apiClient";
import { CurrentUserResponse, LoginRequest, LoginResponse } from "../types/api";

export const authApi = {
  login: (payload: LoginRequest) =>
    request<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: payload,
      skipAuth: true,
    }),
  getCurrentUser: () => request<CurrentUserResponse>("/api/auth/me"),
};
