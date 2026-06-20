import { request } from "./apiClient";
import {
  DashboardStatsResponse,
  ExpiringMembershipResponse,
  SystemNotificationResponse,
} from "../types/api";

export const dashboardApi = {
  getStats: () => request<DashboardStatsResponse>("/api/dashboard/stats"),
  getExpiringMemberships: () =>
    request<ExpiringMembershipResponse[]>("/api/dashboard/expiring-memberships"),
  getNotifications: () => request<SystemNotificationResponse[]>("/api/dashboard/notifications"),
};
