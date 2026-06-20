export type UserRole = "Admin" | "Member";
export type MembershipPlanType = "TimeBased" | "VisitBased" | "Combined";
export type SystemNotificationType = "Info" | "Warning" | "Report";

export interface ApiErrorResponse {
  statusCode: number;
  message: string;
  errors?: Record<string, string[]>;
  timestamp: string;
  path: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface CurrentUserResponse {
  id: number;
  email: string;
  role: UserRole;
  memberId: number | null;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: CurrentUserResponse;
}

export interface MemberResponse {
  id: number;
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  membershipCode: string;
  isActive: boolean;
  createdAt: string;
}

export type MemberDetailsResponse = MemberResponse;

export interface CreateMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  password: string;
}

export interface UpdateMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
}

export interface MembershipPlanResponse {
  id: number;
  name: string;
  description: string | null;
  price: number;
  planType: MembershipPlanType;
  durationInDays: number | null;
  includedVisits: number | null;
  isActive: boolean;
}

export interface CreateTimeBasedPlanRequest {
  name: string;
  description: string;
  price: number;
  durationInDays: number;
}

export interface CreateVisitBasedPlanRequest {
  name: string;
  description: string;
  price: number;
  includedVisits: number;
}

export interface CreateCombinedPlanRequest {
  name: string;
  description: string;
  price: number;
  durationInDays: number;
  includedVisits: number;
}

export type UpdateTimeBasedPlanRequest = CreateTimeBasedPlanRequest;
export type UpdateVisitBasedPlanRequest = CreateVisitBasedPlanRequest;
export type UpdateCombinedPlanRequest = CreateCombinedPlanRequest;

export interface CreateMembershipPaymentRequest {
  memberId: number;
  membershipPlanId: number;
  validFrom: string;
  note: string;
}

export interface MembershipPaymentResponse {
  id: number;
  memberId: number;
  memberFullName: string;
  membershipPlanId: number;
  planName: string;
  planType: MembershipPlanType;
  amount: number;
  paidAt: string;
  validFrom: string;
  validUntil: string | null;
  totalVisits: number | null;
  usedVisits: number | null;
  remainingVisits: number | null;
  note: string | null;
}

export interface MembershipStatusResponse {
  memberId: number;
  memberFullName: string;
  membershipCode: string;
  hasActiveMembership: boolean;
  activePaymentId: number | null;
  planName: string | null;
  planType: MembershipPlanType | null;
  validFrom: string | null;
  validUntil: string | null;
  totalVisits: number | null;
  usedVisits: number | null;
  remainingVisits: number | null;
  message: string;
}

export interface CreateCheckInByMemberIdRequest {
  note: string;
}

export interface CreateCheckInByCodeRequest {
  note: string;
}

export interface CheckInResponse {
  id: number;
  memberId: number;
  memberFullName: string;
  membershipPaymentId: number;
  planName: string;
  checkedInAt: string;
  wasMembershipValid: boolean;
  remainingVisits: number | null;
  message: string;
}

export interface DashboardStatsResponse {
  totalMembers: number;
  activeMembers: number;
  inactiveMembers: number;
  activeMemberships: number;
  expiredMemberships: number;
  todayCheckIns: number;
  currentMonthPayments: number;
  currentMonthRevenue: number;
  expiringInNextSevenDays: number;
}

export interface ExpiringMembershipResponse {
  memberId: number;
  memberFullName: string;
  membershipCode: string;
  membershipPaymentId: number;
  planName: string;
  planType: MembershipPlanType;
  validUntil: string;
  daysUntilExpiration: number;
  remainingVisits: number | null;
}

export interface SystemNotificationResponse {
  id: number;
  title: string;
  message: string;
  type: SystemNotificationType;
  isRead: boolean;
  createdAt: string;
}
