import { request } from "./apiClient";
import {
  CheckInResponse,
  CreateCheckInByCodeRequest,
  CreateCheckInByMemberIdRequest,
} from "../types/api";

export const checkInsApi = {
  getCheckIns: () => request<CheckInResponse[]>("/api/check-ins"),
  getCheckInsForMember: (memberId: number) =>
    request<CheckInResponse[]>(`/api/check-ins/member/${memberId}`),
  createCheckInByMemberId: (memberId: number, payload: CreateCheckInByMemberIdRequest) =>
    request<CheckInResponse>(`/api/check-ins/member/${memberId}`, {
      method: "POST",
      body: payload,
    }),
  createCheckInByCode: (membershipCode: string, payload: CreateCheckInByCodeRequest) =>
    request<CheckInResponse>(`/api/check-ins/code/${encodeURIComponent(membershipCode)}`, {
      method: "POST",
      body: payload,
    }),
  getMyCheckIns: () => request<CheckInResponse[]>("/api/check-ins/me"),
};
