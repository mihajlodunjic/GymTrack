import { MembershipPlanType, SystemNotificationType, UserRole } from "../types/api";

export const formatDate = (value?: string | null) => {
  if (!value) {
    return "-";
  }

  return new Intl.DateTimeFormat("sr-RS", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date(value));
};

export const formatDateTime = (value?: string | null) => {
  if (!value) {
    return "-";
  }

  return new Intl.DateTimeFormat("sr-RS", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
};

export const formatCurrency = (value?: number | null) => {
  if (value === null || value === undefined) {
    return "-";
  }

  return new Intl.NumberFormat("sr-RS", {
    style: "currency",
    currency: "RSD",
    maximumFractionDigits: 2,
  }).format(value);
};

export const formatPlanType = (planType?: MembershipPlanType | null) => {
  switch (planType) {
    case "TimeBased":
      return "Time-based";
    case "VisitBased":
      return "Visit-based";
    case "Combined":
      return "Combined";
    default:
      return "-";
  }
};

export const formatNotificationType = (type: SystemNotificationType) => {
  switch (type) {
    case "Info":
      return "Info";
    case "Warning":
      return "Warning";
    case "Report":
      return "Report";
    default:
      return type;
  }
};

export const roleHomePath = (role: UserRole) => (role === "Admin" ? "/admin" : "/member");

export const joinClassNames = (...names: Array<string | false | null | undefined>) =>
  names.filter(Boolean).join(" ");

export const todayInputValue = () => new Date().toISOString().slice(0, 10);
