import { FormEvent, useEffect, useState } from "react";
import { getApiErrorMessages } from "../../api/apiClient";
import { plansApi } from "../../api/plansApi";
import { DataTable } from "../../components/DataTable";
import { ErrorAlert } from "../../components/ErrorAlert";
import { LoadingState } from "../../components/LoadingState";
import { PageHeader } from "../../components/PageHeader";
import { StatusBadge } from "../../components/StatusBadge";
import { MembershipPlanResponse, MembershipPlanType } from "../../types/api";
import { formatCurrency, formatPlanType } from "../../utils/format";

type FormMode = "create" | "edit" | null;

interface PlanFormValues {
  name: string;
  description: string;
  price: string;
  durationInDays: string;
  includedVisits: string;
}

const emptyPlanForm: PlanFormValues = {
  name: "",
  description: "",
  price: "",
  durationInDays: "",
  includedVisits: "",
};

export const PlansPage = () => {
  const [plans, setPlans] = useState<MembershipPlanResponse[]>([]);
  const [formMode, setFormMode] = useState<FormMode>(null);
  const [selectedPlan, setSelectedPlan] = useState<MembershipPlanResponse | null>(null);
  const [selectedType, setSelectedType] = useState<MembershipPlanType>("TimeBased");
  const [formValues, setFormValues] = useState<PlanFormValues>(emptyPlanForm);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadPlans = async () => {
    try {
      setIsLoading(true);
      setErrorMessages([]);
      const response = await plansApi.getPlans();
      setPlans(response);
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadPlans();
  }, []);

  const openCreate = () => {
    setSelectedPlan(null);
    setSelectedType("TimeBased");
    setFormValues(emptyPlanForm);
    setFormMode("create");
  };

  const openEdit = (plan: MembershipPlanResponse) => {
    setSelectedPlan(plan);
    setSelectedType(plan.planType);
    setFormValues({
      name: plan.name,
      description: plan.description || "",
      price: String(plan.price),
      durationInDays: plan.durationInDays ? String(plan.durationInDays) : "",
      includedVisits: plan.includedVisits ? String(plan.includedVisits) : "",
    });
    setFormMode("edit");
  };

  const closeForm = () => {
    setSelectedPlan(null);
    setFormMode(null);
    setFormValues(emptyPlanForm);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessages([]);

    try {
      const commonPayload = {
        name: formValues.name,
        description: formValues.description,
        price: Number(formValues.price),
      };

      if (formMode === "create") {
        if (selectedType === "TimeBased") {
          await plansApi.createTimeBasedPlan({
            ...commonPayload,
            durationInDays: Number(formValues.durationInDays),
          });
        } else if (selectedType === "VisitBased") {
          await plansApi.createVisitBasedPlan({
            ...commonPayload,
            includedVisits: Number(formValues.includedVisits),
          });
        } else {
          await plansApi.createCombinedPlan({
            ...commonPayload,
            durationInDays: Number(formValues.durationInDays),
            includedVisits: Number(formValues.includedVisits),
          });
        }
      } else if (selectedPlan) {
        if (selectedPlan.planType === "TimeBased") {
          await plansApi.updateTimeBasedPlan(selectedPlan.id, {
            ...commonPayload,
            durationInDays: Number(formValues.durationInDays),
          });
        } else if (selectedPlan.planType === "VisitBased") {
          await plansApi.updateVisitBasedPlan(selectedPlan.id, {
            ...commonPayload,
            includedVisits: Number(formValues.includedVisits),
          });
        } else {
          await plansApi.updateCombinedPlan(selectedPlan.id, {
            ...commonPayload,
            durationInDays: Number(formValues.durationInDays),
            includedVisits: Number(formValues.includedVisits),
          });
        }
      }

      closeForm();
      await loadPlans();
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeactivate = async (plan: MembershipPlanResponse) => {
    if (!window.confirm(`Deactivate membership plan "${plan.name}"?`)) {
      return;
    }

    try {
      setErrorMessages([]);
      await plansApi.deactivatePlan(plan.id);
      await loadPlans();
    } catch (error) {
      setErrorMessages(getApiErrorMessages(error));
    }
  };

  const currentType = formMode === "edit" && selectedPlan ? selectedPlan.planType : selectedType;

  return (
    <div className="page-stack">
      <PageHeader
        title="Membership Plans"
        description="Manage time-based, visit-based and combined membership plans."
        actions={
          <button className="button button-primary" onClick={openCreate} type="button">
            New plan
          </button>
        }
      />

      <ErrorAlert title="Plans action failed" messages={errorMessages} />

      <div className="page-grid">
        <section className="panel panel-main">
          {isLoading ? (
            <LoadingState label="Loading plans..." />
          ) : (
            <DataTable
              rows={plans}
              emptyTitle="No plans found"
              emptyDescription="Create a membership plan to start selling memberships."
              columns={[
                {
                  key: "name",
                  header: "Name",
                  render: (plan) => (
                    <div>
                      <strong>{plan.name}</strong>
                      <div className="table-subtext">{plan.description || "No description"}</div>
                    </div>
                  ),
                },
                { key: "type", header: "Type", render: (plan) => formatPlanType(plan.planType) },
                { key: "price", header: "Price", render: (plan) => formatCurrency(plan.price) },
                { key: "duration", header: "Duration (days)", render: (plan) => plan.durationInDays ?? "-" },
                { key: "visits", header: "Included visits", render: (plan) => plan.includedVisits ?? "-" },
                {
                  key: "status",
                  header: "Status",
                  render: (plan) => (
                    <StatusBadge label={plan.isActive ? "Active" : "Inactive"} tone={plan.isActive ? "success" : "danger"} />
                  ),
                },
                {
                  key: "actions",
                  header: "Actions",
                  render: (plan) => (
                    <div className="table-actions">
                      <button className="button button-secondary button-small" onClick={() => openEdit(plan)} type="button">
                        Edit
                      </button>
                      <button className="button button-danger button-small" onClick={() => void handleDeactivate(plan)} type="button">
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
              <h2>{formMode === "create" ? "Create plan" : "Edit plan"}</h2>
              <button className="button button-secondary button-small" onClick={closeForm} type="button">
                Close
              </button>
            </div>

            <form className="form-grid" onSubmit={handleSubmit}>
              <label className="field field-full">
                <span>Plan type</span>
                <select
                  disabled={formMode === "edit"}
                  value={currentType}
                  onChange={(event) => setSelectedType(event.target.value as MembershipPlanType)}
                >
                  <option value="TimeBased">Time-based</option>
                  <option value="VisitBased">Visit-based</option>
                  <option value="Combined">Combined</option>
                </select>
              </label>
              <label className="field field-full">
                <span>Name</span>
                <input
                  value={formValues.name}
                  onChange={(event) => setFormValues((current) => ({ ...current, name: event.target.value }))}
                  required
                />
              </label>
              <label className="field field-full">
                <span>Description</span>
                <textarea
                  rows={4}
                  value={formValues.description}
                  onChange={(event) =>
                    setFormValues((current) => ({ ...current, description: event.target.value }))
                  }
                />
              </label>
              <label className="field">
                <span>Price</span>
                <input
                  min="0.01"
                  step="0.01"
                  type="number"
                  value={formValues.price}
                  onChange={(event) => setFormValues((current) => ({ ...current, price: event.target.value }))}
                  required
                />
              </label>
              {currentType !== "VisitBased" ? (
                <label className="field">
                  <span>Duration in days</span>
                  <input
                    min="1"
                    step="1"
                    type="number"
                    value={formValues.durationInDays}
                    onChange={(event) =>
                      setFormValues((current) => ({ ...current, durationInDays: event.target.value }))
                    }
                    required
                  />
                </label>
              ) : null}
              {currentType !== "TimeBased" ? (
                <label className="field">
                  <span>Included visits</span>
                  <input
                    min="1"
                    step="1"
                    type="number"
                    value={formValues.includedVisits}
                    onChange={(event) =>
                      setFormValues((current) => ({ ...current, includedVisits: event.target.value }))
                    }
                    required
                  />
                </label>
              ) : null}
              <div className="form-actions field-full">
                <button className="button button-primary" disabled={isSubmitting} type="submit">
                  {isSubmitting ? "Saving..." : formMode === "create" ? "Create plan" : "Update plan"}
                </button>
              </div>
            </form>
          </section>
        ) : null}
      </div>
    </div>
  );
};
