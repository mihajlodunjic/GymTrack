import { request } from "./apiClient";
import {
  CreateMemberRequest,
  MemberDetailsResponse,
  MemberResponse,
  MembershipStatusResponse,
  UpdateMemberRequest,
} from "../types/api";

interface MemberQueryParams {
  search?: string;
  isActive?: boolean | "";
}

const buildMemberQuery = ({ search, isActive }: MemberQueryParams) => {
  const params = new URLSearchParams();

  if (search?.trim()) {
    params.set("search", search.trim());
  }

  if (typeof isActive === "boolean") {
    params.set("isActive", String(isActive));
  }

  const query = params.toString();
  return query ? `?${query}` : "";
};

export const membersApi = {
  getMembers: (params: MemberQueryParams = {}) =>
    request<MemberResponse[]>(`/api/members${buildMemberQuery(params)}`),
  getMemberById: (id: number) => request<MemberDetailsResponse>(`/api/members/${id}`),
  createMember: (payload: CreateMemberRequest) =>
    request<MemberDetailsResponse>("/api/members", {
      method: "POST",
      body: payload,
    }),
  updateMember: (id: number, payload: UpdateMemberRequest) =>
    request<MemberDetailsResponse>(`/api/members/${id}`, {
      method: "PUT",
      body: payload,
    }),
  deactivateMember: (id: number) =>
    request<void>(`/api/members/${id}`, {
      method: "DELETE",
      responseType: "void",
    }),
  getMemberStatus: (id: number) => request<MembershipStatusResponse>(`/api/members/${id}/status`),
  getCurrentMember: () => request<MemberDetailsResponse>("/api/members/me"),
  getCurrentMemberStatus: () => request<MembershipStatusResponse>("/api/members/me/status"),
  getMemberQrCode: (id: number) =>
    request<Blob>(`/api/members/${id}/qr-code`, {
      responseType: "blob",
    }),
  getCurrentMemberQrCode: () =>
    request<Blob>("/api/members/me/qr-code", {
      responseType: "blob",
    }),
};
