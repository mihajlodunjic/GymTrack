import { FormEvent, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getApiErrorMessages } from "../../api/apiClient";
import { membersApi } from "../../api/membersApi";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { StatusBadge } from "../../components/StatusBadge";
import {
  CreateMemberRequest,
  MemberDetailsResponse,
  MemberResponse,
  MembershipStatusResponse,
  UpdateMemberRequest,
} from "../../types/api";
import { formatDateTime } from "../../utils/format";

type FilterValue = "" | "true" | "false";
type FormMode = "create" | "edit" | null;

const emptyCreateForm: CreateMemberRequest = {
  firstName: "",
  lastName: "",
  email: "",
  phoneNumber: "",
  password: "",
};

const emptyUpdateForm: UpdateMemberRequest = {
  firstName: "",
  lastName: "",
  email: "",
  phoneNumber: "",
};

export const MembersPage = () => {
  const [members, setMembers] = useState<MemberResponse[]>([]);
  const [statusMap, setStatusMap] = useState<Record<number, MembershipStatusResponse>>({});
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<FilterValue>("");
  const [formMode, setFormMode] = useState<FormMode>(null);
  const [selectedMember, setSelectedMember] = useState<MemberDetailsResponse | null>(null);
  const [createForm, setCreateForm] = useState<CreateMemberRequest>(emptyCreateForm);
  const [updateForm, setUpdateForm] = useState<UpdateMemberRequest>(emptyUpdateForm);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadMembers = async () => {
    try {
      setIsLoading(true);
      setErrorMessages([]);

      const isActive =
        activeFilter === "" ? "" : activeFilter === "true";

      const membersResponse = await membersApi.getMembers({
        search,
        isActive,
      });

      const statuses = await Promise.all(
        membersResponse.map(async (member) => [member.id, await membersApi.getMemberStatus(member.id)] as const)
      );

      setMembers(membersResponse);
      setStatusMap(Object.fromEntries(statuses));
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadMembers();
  }, []);

  const openCreate = () => {
    setSelectedMember(null);
    setCreateForm(emptyCreateForm);
    setFormMode("create");
    setErrorMessages([]);
  };

  const openEdit = async (memberId: number) => {
    try {
      setErrorMessages([]);
      const member = await membersApi.getMemberById(memberId);
      setSelectedMember(member);
      setUpdateForm({
        firstName: member.firstName,
        lastName: member.lastName,
        email: member.email,
        phoneNumber: member.phoneNumber || "",
      });
      setFormMode("edit");
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    }
  };

  const closeForm = () => {
    setSelectedMember(null);
    setFormMode(null);
    setCreateForm(emptyCreateForm);
    setUpdateForm(emptyUpdateForm);
  };

  const handleFilterSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await loadMembers();
  };

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    try {
      setIsSubmitting(true);
      setErrorMessages([]);
      await membersApi.createMember(createForm);
      closeForm();
      await loadMembers();
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleUpdate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedMember) {
      return;
    }

    try {
      setIsSubmitting(true);
      setErrorMessages([]);
      await membersApi.updateMember(selectedMember.id, updateForm);
      closeForm();
      await loadMembers();
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeactivate = async (member: MemberResponse) => {
    if (!window.confirm(`Deactivate member ${member.firstName} ${member.lastName}?`)) {
      return;
    }

    try {
      setErrorMessages([]);
      await membersApi.deactivateMember(member.id);
      await loadMembers();
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    }
  };

  return (
    <div className="page-stack">
      <PageHeader
        title="Members"
        description="Create, update and review member accounts, membership codes and current membership status."
        actions={
          <button className="button button-primary" onClick={openCreate} type="button">
            New member
          </button>
        }
      />

      <ErrorAlert title="Members action failed" messages={errorMessages} />

      <section className="panel">
        <form className="filters-row" onSubmit={handleFilterSubmit}>
          <label className="field field-grow">
            <span>Search</span>
            <input
              placeholder="Name, email, phone or membership code"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </label>
          <label className="field field-small">
            <span>Status</span>
            <select value={activeFilter} onChange={(event) => setActiveFilter(event.target.value as FilterValue)}>
              <option value="">All</option>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>
          </label>
          <div className="filters-actions">
            <button className="button button-secondary" type="submit">
              Apply filters
            </button>
          </div>
        </form>
      </section>

      <div className="page-grid">
        <section className="panel panel-main">
          {isLoading ? (
            <LoadingState label="Loading members..." />
          ) : (
            <DataTable
              rows={members}
              emptyTitle="No members found"
              emptyDescription="Adjust filters or create a new member."
              columns={[
                {
                  key: "member",
                  header: "Member",
                  render: (member) => (
                    <div>
                      <strong>
                        {member.firstName} {member.lastName}
                      </strong>
                      <div className="table-subtext">{member.email}</div>
                    </div>
                  ),
                },
                {
                  key: "contact",
                  header: "Phone",
                  render: (member) => member.phoneNumber || "-",
                },
                {
                  key: "membershipCode",
                  header: "Membership code",
                  render: (member) => (
                    <code className="code-inline">{member.membershipCode}</code>
                  ),
                },
                {
                  key: "membershipStatus",
                  header: "Membership status",
                  render: (member) =>
                    statusMap[member.id]?.hasActiveMembership ? (
                      <StatusBadge label="Active membership" tone="success" />
                    ) : (
                      <StatusBadge label="No active membership" tone="warning" />
                    ),
                },
                {
                  key: "accountStatus",
                  header: "Account",
                  render: (member) => (
                    <StatusBadge label={member.isActive ? "Active" : "Inactive"} tone={member.isActive ? "success" : "danger"} />
                  ),
                },
                {
                  key: "createdAt",
                  header: "Created",
                  render: (member) => formatDateTime(member.createdAt),
                },
                {
                  key: "actions",
                  header: "Actions",
                  render: (member) => (
                    <div className="table-actions">
                      <Link className="button button-secondary button-small" to={`/admin/members/${member.id}`}>
                        Details / QR
                      </Link>
                      <button className="button button-secondary button-small" onClick={() => void openEdit(member.id)} type="button">
                        Edit
                      </button>
                      <button className="button button-danger button-small" onClick={() => void handleDeactivate(member)} type="button">
                        Deactivate
                      </button>
                    </div>
                  ),
                },
              ]}
            />
          )}
        </section>

        {formMode ? (
          <section className="panel panel-side">
            <div className="section-heading">
              <h2>{formMode === "create" ? "Create member" : "Edit member"}</h2>
              <button className="button button-secondary button-small" onClick={closeForm} type="button">
                Close
              </button>
            </div>

            {formMode === "create" ? (
              <form className="form-grid" onSubmit={handleCreate}>
                <label className="field">
                  <span>First name</span>
                  <input
                    value={createForm.firstName}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, firstName: event.target.value }))
                    }
                    required
                  />
                </label>
                <label className="field">
                  <span>Last name</span>
                  <input
                    value={createForm.lastName}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, lastName: event.target.value }))
                    }
                    required
                  />
                </label>
                <label className="field field-full">
                  <span>Email</span>
                  <input
                    type="email"
                    value={createForm.email}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, email: event.target.value }))
                    }
                    required
                  />
                </label>
                <label className="field">
                  <span>Phone number</span>
                  <input
                    value={createForm.phoneNumber}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, phoneNumber: event.target.value }))
                    }
                  />
                </label>
                <label className="field">
                  <span>Password</span>
                  <input
                    type="password"
                    value={createForm.password}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, password: event.target.value }))
                    }
                    minLength={6}
                    required
                  />
                </label>
                <div className="form-actions field-full">
                  <button className="button button-primary" disabled={isSubmitting} type="submit">
                    {isSubmitting ? "Saving..." : "Create member"}
                  </button>
                </div>
              </form>
            ) : (
              <form className="form-grid" onSubmit={handleUpdate}>
                <label className="field">
                  <span>First name</span>
                  <input
                    value={updateForm.firstName}
                    onChange={(event) =>
                      setUpdateForm((current) => ({ ...current, firstName: event.target.value }))
                    }
                    required
                  />
                </label>
                <label className="field">
                  <span>Last name</span>
                  <input
                    value={updateForm.lastName}
                    onChange={(event) =>
                      setUpdateForm((current) => ({ ...current, lastName: event.target.value }))
                    }
                    required
                  />
                </label>
                <label className="field field-full">
                  <span>Email</span>
                  <input
                    type="email"
                    value={updateForm.email}
                    onChange={(event) =>
                      setUpdateForm((current) => ({ ...current, email: event.target.value }))
                    }
                    required
                  />
                </label>
                <label className="field field-full">
                  <span>Phone number</span>
                  <input
                    value={updateForm.phoneNumber}
                    onChange={(event) =>
                      setUpdateForm((current) => ({ ...current, phoneNumber: event.target.value }))
                    }
                  />
                </label>
                <div className="form-actions field-full">
                  <button className="button button-primary" disabled={isSubmitting} type="submit">
                    {isSubmitting ? "Saving..." : "Update member"}
                  </button>
                </div>
              </form>
            )}
          </section>
        ) : null}
      </div>
    </div>
  );
};
