import { useEffect, useState } from "react";
import { dashboardApi } from "../../api/dashboardApi";
import { getApiErrorMessages } from "../../api/apiClient";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { StatCard } from "../../components/StatCard";
import { StatusBadge } from "../../components/StatusBadge";
import {
  DashboardStatsResponse,
  ExpiringMembershipResponse,
  SystemNotificationResponse,
} from "../../types/api";
import {
  formatCurrency,
  formatDate,
  formatDateTime,
  formatNotificationType,
  formatPlanType,
} from "../../utils/format";

export const AdminDashboardPage = () => {
  const [stats, setStats] = useState<DashboardStatsResponse | null>(null);
  const [expiringMemberships, setExpiringMemberships] = useState<ExpiringMembershipResponse[]>([]);
  const [notifications, setNotifications] = useState<SystemNotificationResponse[]>([]);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const loadDashboard = async () => {
      try {
        setIsLoading(true);
        setErrorMessages([]);

        const [statsResponse, expiringResponse, notificationsResponse] = await Promise.all([
          dashboardApi.getStats(),
          dashboardApi.getExpiringMemberships(),
          dashboardApi.getNotifications(),
        ]);

        if (isMounted) {
          setStats(statsResponse);
          setExpiringMemberships(expiringResponse);
          setNotifications(notificationsResponse);
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessages(getApiErrorMessages(error));
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void loadDashboard();

    return () => {
      isMounted = false;
    };
  }, []);

  if (isLoading) {
    return <LoadingState label="Loading dashboard..." />;
  }

  return (
    <div className="page-stack">
      <PageHeader
        title="Admin Dashboard"
        description="Overview of members, memberships, payments, expiring plans and notifications."
      />

      <ErrorAlert title="Dashboard could not be loaded" messages={errorMessages} />

      {stats ? (
        <div className="stats-grid">
          <StatCard label="Total members" value={stats.totalMembers} />
          <StatCard label="Active members" value={stats.activeMembers} />
          <StatCard label="Inactive members" value={stats.inactiveMembers} />
          <StatCard label="Active memberships" value={stats.activeMemberships} />
          <StatCard label="Expired memberships" value={stats.expiredMemberships} />
          <StatCard label="Today's check-ins" value={stats.todayCheckIns} />
          <StatCard label="Monthly payments" value={stats.currentMonthPayments} />
          <StatCard label="Monthly revenue" value={formatCurrency(stats.currentMonthRevenue)} />
          <StatCard label="Expiring in 7 days" value={stats.expiringInNextSevenDays} />
        </div>
      ) : null}

      <section className="panel">
        <div className="section-heading">
          <h2>Expiring memberships</h2>
        </div>
        <DataTable
          rows={expiringMemberships}
          emptyTitle="No expiring memberships"
          emptyDescription="There are no memberships expiring in the next seven days."
          columns={[
            {
              key: "member",
              header: "Member",
              render: (item) => (
                <div>
                  <strong>{item.memberFullName}</strong>
                  <div className="table-subtext">{item.membershipCode}</div>
                </div>
              ),
            },
            { key: "plan", header: "Plan", render: (item) => item.planName },
            { key: "type", header: "Type", render: (item) => formatPlanType(item.planType) },
            { key: "validUntil", header: "Valid until", render: (item) => formatDate(item.validUntil) },
            {
              key: "days",
              header: "Days left",
              render: (item) => <StatusBadge label={`${item.daysUntilExpiration} days`} tone="warning" />,
            },
            {
              key: "visits",
              header: "Remaining visits",
              render: (item) => item.remainingVisits ?? "-",
            },
          ]}
        />
      </section>

      <section className="panel">
        <div className="section-heading">
          <h2>Notifications</h2>
        </div>
        <DataTable
          rows={notifications}
          emptyTitle="No notifications"
          emptyDescription="System notifications will appear here."
          columns={[
            {
              key: "title",
              header: "Title",
              render: (item) => (
                <div>
                  <strong>{item.title}</strong>
                  <div className="table-subtext">{item.message}</div>
                </div>
              ),
            },
            {
              key: "type",
              header: "Type",
              render: (item) => (
                <StatusBadge
                  label={formatNotificationType(item.type)}
                  tone={item.type === "Warning" ? "warning" : "neutral"}
                />
              ),
            },
            {
              key: "status",
              header: "Status",
              render: (item) => (
                <StatusBadge label={item.isRead ? "Read" : "Unread"} tone={item.isRead ? "neutral" : "success"} />
              ),
            },
            { key: "createdAt", header: "Created", render: (item) => formatDateTime(item.createdAt) },
          ]}
        />
      </section>
    </div>
  );
};
