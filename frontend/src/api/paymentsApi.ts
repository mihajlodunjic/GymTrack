import { request } from "./apiClient";
import { CreateMembershipPaymentRequest, MembershipPaymentResponse } from "../types/api";

export const paymentsApi = {
  getPayments: () => request<MembershipPaymentResponse[]>("/api/membership-payments"),
  createPayment: (payload: CreateMembershipPaymentRequest) =>
    request<MembershipPaymentResponse>("/api/membership-payments", {
      method: "POST",
      body: payload,
    }),
  getPaymentsForMember: (memberId: number) =>
    request<MembershipPaymentResponse[]>(`/api/membership-payments/member/${memberId}`),
  getMyPayments: () => request<MembershipPaymentResponse[]>("/api/membership-payments/me"),
};
