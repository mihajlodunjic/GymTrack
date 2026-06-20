import { request } from "./apiClient";
import {
  CreateCombinedPlanRequest,
  CreateTimeBasedPlanRequest,
  CreateVisitBasedPlanRequest,
  MembershipPlanResponse,
  UpdateCombinedPlanRequest,
  UpdateTimeBasedPlanRequest,
  UpdateVisitBasedPlanRequest,
} from "../types/api";

export const plansApi = {
  getPlans: () => request<MembershipPlanResponse[]>("/api/membership-plans"),
  createTimeBasedPlan: (payload: CreateTimeBasedPlanRequest) =>
    request<MembershipPlanResponse>("/api/membership-plans/time-based", {
      method: "POST",
      body: payload,
    }),
  createVisitBasedPlan: (payload: CreateVisitBasedPlanRequest) =>
    request<MembershipPlanResponse>("/api/membership-plans/visit-based", {
      method: "POST",
      body: payload,
    }),
  createCombinedPlan: (payload: CreateCombinedPlanRequest) =>
    request<MembershipPlanResponse>("/api/membership-plans/combined", {
      method: "POST",
      body: payload,
    }),
  updateTimeBasedPlan: (id: number, payload: UpdateTimeBasedPlanRequest) =>
    request<MembershipPlanResponse>(`/api/membership-plans/time-based/${id}`, {
      method: "PUT",
      body: payload,
    }),
  updateVisitBasedPlan: (id: number, payload: UpdateVisitBasedPlanRequest) =>
    request<MembershipPlanResponse>(`/api/membership-plans/visit-based/${id}`, {
      method: "PUT",
      body: payload,
    }),
  updateCombinedPlan: (id: number, payload: UpdateCombinedPlanRequest) =>
    request<MembershipPlanResponse>(`/api/membership-plans/combined/${id}`, {
      method: "PUT",
      body: payload,
    }),
  deactivatePlan: (id: number) =>
    request<void>(`/api/membership-plans/${id}`, {
      method: "DELETE",
      responseType: "void",
    }),
};
