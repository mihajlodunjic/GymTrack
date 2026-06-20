import { joinClassNames } from "../utils/format";

interface StatusBadgeProps {
  label: string;
  tone?: "neutral" | "success" | "warning" | "danger";
}

export const StatusBadge = ({ label, tone = "neutral" }: StatusBadgeProps) => (
  <span className={joinClassNames("badge", `badge-${tone}`)}>{label}</span>
);
