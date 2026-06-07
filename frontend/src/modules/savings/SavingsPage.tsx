import { apiFetch } from '@/api/client';
import {
  GoalContributionSchema,
  SavingsGoalProgressListSchema,
  SavingsGoalSchema,
} from '@/api/schemas';
import type { AddContributionRequest, CreateGoalRequest, SavingsGoalProgress } from '@/api/types';
import { useCurrencyFormatter } from '@/hooks/useCurrencyFormatter';
import { type FormEvent, useEffect, useMemo, useRef, useState } from 'react';
import GoalProgressSection from './GoalProgressSection';
import SavingsFormsSection, {
  type ContributionForm,
  type ContributionMode,
  type GoalForm,
} from './SavingsFormsSection';

// Converts a Date to a YYYY-MM-DD string in the user's LOCAL timezone (not UTC)
// This prevents date-picker values from shifting to the previous day in negative UTC offsets
const toDateInputValue = (date: Date) => {
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return localDate.toISOString().slice(0, 10);
};

// Returns a YYYY-MM-DD string for a date that is `days` days from today
const daysFromToday = (days: number) => {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return toDateInputValue(date);
};

// Shorthand for today's date as a date-input value
const today = () => daysFromToday(0);

// Blank form state for creating a new savings goal
const initialGoalForm: GoalForm = {
  name: '',
  targetAmount: '',
  deadline: '',
  description: '',
};

// Blank form state for a contribution (deposit or withdrawal)
const initialForm: ContributionForm = {
  goalId: '',
  amount: '',
  date: today(),
  note: '',
  reason: '',
};

// The backend may return a status as a numeric enum value; this array maps index → string label
const goalStatusLabels = ['Active', 'Completed', 'Paused', 'Abandoned'] as const;

// Converts a goal status (which may be a number or a string) to a display label
const formatGoalStatus = (status: SavingsGoalProgress['status']): string => {
  if (typeof status === 'number') {
    return goalStatusLabels[status] ?? String(status);
  }

  return status ?? '';
};

// Helper predicates used to determine how a goal can be interacted with
const isCompletedGoal = (goal: SavingsGoalProgress) =>
  formatGoalStatus(goal.status) === 'Completed';

const isPausedGoal = (goal: SavingsGoalProgress) => formatGoalStatus(goal.status) === 'Paused';

const isAbandonedGoal = (goal: SavingsGoalProgress) =>
  formatGoalStatus(goal.status) === 'Abandoned';

// Only active goals (not completed, paused, or abandoned) accept new contributions
const canContributeToGoal = (goal: SavingsGoalProgress) =>
  !isCompletedGoal(goal) && !isPausedGoal(goal) && !isAbandonedGoal(goal);

// Returns the maximum allowed amount for the current contribution mode:
// - deposit: how much is still needed to reach the target
// - withdraw: how much is currently in the goal's balance
const getContributionLimit = (goal: SavingsGoalProgress, mode: ContributionMode) =>
  Math.max(mode === 'withdraw' ? (goal.currentBalance ?? 0) : (goal.amountRemaining ?? 0), 0);

// If the typed amount exceeds the limit for the current mode, clamp it to the maximum
const clampContributionAmount = (
  value: string,
  goal: SavingsGoalProgress | null | undefined,
  mode: ContributionMode,
) => {
  if (!goal || value === '') return value;

  const amount = Number(value);
  if (!Number.isFinite(amount)) return value;

  const limit = getContributionLimit(goal, mode);
  if (amount <= limit) return value;

  // If the limit is 0, clear the field; otherwise cap it at the limit
  return limit > 0 ? String(limit) : '';
};

// Determines which goal ID should be selected in the contribution form after a data refresh.
// Priority: preferredGoalId (e.g. newly created goal) > currently selected goal > first in list
const getSelectableGoalId = (
  goals: SavingsGoalProgress[],
  currentGoalId: string,
  preferredGoalId?: string,
) => {
  const selectableGoals = goals.filter(canContributeToGoal);
  const preferredGoal = selectableGoals.find((goal) => goal.id === preferredGoalId);
  const currentGoal = selectableGoals.find((goal) => goal.id === currentGoalId);

  return preferredGoal?.id ?? currentGoal?.id ?? selectableGoals[0]?.id ?? '';
};

// Page for managing savings goals — create goals and record deposits or withdrawals
export default function SavingsPage() {
  const fmt = useCurrencyFormatter();
  // Ref used to move keyboard focus to the amount field when switching to withdrawal mode
  const amountInputRef = useRef<HTMLInputElement>(null);
  // Goals must have a deadline at least 30 days in the future
  const minimumGoalDeadline = daysFromToday(30);
  const [goals, setGoals] = useState<SavingsGoalProgress[]>([]);
  const [goalForm, setGoalForm] = useState<GoalForm>(initialGoalForm);
  const [form, setForm] = useState<ContributionForm>(initialForm);
  // contributionMode toggles between deposit and withdrawal
  const [contributionMode, setContributionMode] = useState<ContributionMode>('deposit');
  const [loading, setLoading] = useState(true);
  const [creatingGoal, setCreatingGoal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Derive the currently selected goal object from the goalId in the contribution form
  const selectedGoal = useMemo(
    () => goals.find((goal) => goal.id === form.goalId) ?? null,
    [form.goalId, goals], // Re-compute when the selected ID or the goals list changes
  );
  // Only goals that can accept contributions are shown in the dropdown
  const selectableGoals = useMemo(() => goals.filter(canContributeToGoal), [goals]);
  // Pre-compute the contribution limit so it can be displayed next to the amount field
  const contributionLimit = selectedGoal
    ? getContributionLimit(selectedGoal, contributionMode)
    : null;

  // Refreshes the goal list and updates the contribution form's selected goal.
  // preferredGoalId is used after creating a new goal so it is auto-selected
  const loadGoals = async (preferredGoalId?: string) => {
    const data = await apiFetch('/api/goals', SavingsGoalProgressListSchema);
    setGoals(data);
    setForm((current) => ({
      ...current,
      goalId: getSelectableGoalId(data, current.goalId, preferredGoalId),
    }));
  };

  // Fetches goals on mount; uses a cancelled flag to avoid state updates after unmount
  useEffect(() => {
    let cancelled = false;

    const fetchGoals = async () => {
      try {
        const data = await apiFetch('/api/goals', SavingsGoalProgressListSchema);
        if (cancelled) return;
        setGoals(data);
        // Pre-select the first selectable goal in the contribution form
        setForm((current) => ({
          ...current,
          goalId: getSelectableGoalId(data, current.goalId),
        }));
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Unable to load savings goals');
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchGoals();

    // Cleanup: prevent stale state update if the component unmounts mid-request
    return () => {
      cancelled = true;
    };
  }, []); // Empty deps — fetch only once on mount

  // Updates a single field in the new-goal form and clears any success message
  const updateGoalForm = (field: keyof GoalForm, value: string) => {
    setGoalForm((current) => ({ ...current, [field]: value }));
    setSuccess(null);
  };

  // Updates a single field in the contribution form and clears any success message
  const updateForm = (field: keyof ContributionForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setSuccess(null);
  };

  // Switches the selected goal and clamps the existing amount to the new goal's limit
  const updateContributionGoal = (goalId: string) => {
    const nextGoal = goals.find((goal) => goal.id === goalId) ?? null;
    setForm((current) => ({
      ...current,
      goalId,
      // Clamp the amount so it doesn't exceed what's allowed for the new goal
      amount: clampContributionAmount(current.amount, nextGoal, contributionMode),
    }));
    setSuccess(null);
  };

  // Updates the amount field, clamping it to the current goal's contribution limit
  const updateContributionAmount = (value: string) => {
    setForm((current) => ({
      ...current,
      amount: clampContributionAmount(value, selectedGoal, contributionMode),
    }));
    setSuccess(null);
  };

  // Switches deposit/withdraw mode and re-clamps the amount to the new limit for that mode
  const selectContributionMode = (mode: ContributionMode) => {
    setForm((current) => ({
      ...current,
      amount: clampContributionAmount(current.amount, selectedGoal, mode),
    }));
    setContributionMode(mode);
    setSuccess(null);
  };

  // Called from the goal progress list when the user clicks "Withdraw" on a specific goal
  const prepareWithdrawal = (goalId: string) => {
    setContributionMode('withdraw');
    setError(null);
    setSuccess(null);
    setForm((current) => ({
      ...current,
      goalId,
      amount: '',
      date: current.date || today(),
      reason: current.reason,
    }));
    // Move focus to the amount field so the user can type immediately
    amountInputRef.current?.focus();
  };

  // Validates and submits the new savings goal form
  const handleCreateGoal = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const targetAmount = Number(goalForm.targetAmount);
    if (!goalForm.name.trim()) {
      setError('Enter a goal name.');
      return;
    }
    if (!Number.isFinite(targetAmount) || targetAmount <= 0) {
      setError('Enter a target amount greater than zero.');
      return;
    }
    if (!goalForm.deadline) {
      setError('Choose a goal deadline.');
      return;
    }

    setCreatingGoal(true);
    try {
      const goal = await apiFetch('/api/goals', SavingsGoalSchema, {
        method: 'POST',
        body: JSON.stringify({
          name: goalForm.name.trim(),
          targetAmount,
          // Midnight local time converted to UTC ISO string
          deadline: new Date(`${goalForm.deadline}T00:00:00`).toISOString(),
          description: goalForm.description.trim() || null,
        } satisfies CreateGoalRequest),
      });

      setGoalForm(initialGoalForm);
      setSuccess(`Goal "${goal.name}" created.`);
      // Pass the new goal's ID so it is automatically selected in the contribution form
      await loadGoals(goal.id);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to create goal');
    } finally {
      setCreatingGoal(false);
    }
  };

  // Validates and submits a deposit or withdrawal contribution for the selected goal
  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const amount = Math.abs(Number(form.amount)); // Always work with positive amounts for validation
    if (!form.goalId) {
      setError('Choose a savings goal first.');
      return;
    }
    if (!selectedGoal) {
      setError('Choose a valid savings goal.');
      return;
    }
    if (!canContributeToGoal(selectedGoal)) {
      setError('Resume this goal before adding contributions or withdrawals.');
      return;
    }
    if (!Number.isFinite(amount) || amount <= 0) {
      setError('Enter an amount greater than zero.');
      return;
    }
    // Enforce the per-mode limit computed from the goal's balance or remaining amount
    const limit = getContributionLimit(selectedGoal, contributionMode);
    if (amount > limit) {
      setError(
        contributionMode === 'withdraw'
          ? `Withdrawal cannot exceed ${fmt(limit)} available in ${selectedGoal.name}.`
          : `Deposit cannot exceed ${fmt(limit)} remaining for ${selectedGoal.name}.`,
      );
      return;
    }
    if (!form.date) {
      setError('Choose a contribution date.');
      return;
    }
    // Withdrawals require a reason so there is an audit trail
    if (contributionMode === 'withdraw' && !form.reason.trim()) {
      setError('Enter a withdrawal reason.');
      return;
    }

    setSubmitting(true);
    try {
      // Withdrawals are stored as negative amounts; deposits are positive
      const signedAmount = contributionMode === 'withdraw' ? -amount : amount;
      await apiFetch(`/api/goals/${form.goalId}/contributions`, GoalContributionSchema, {
        method: 'POST',
        body: JSON.stringify({
          amount: signedAmount,
          date: new Date(`${form.date}T00:00:00`).toISOString(),
          // Send only the relevant extra field for the mode; the other is null
          reason: contributionMode === 'withdraw' ? form.reason.trim() : null,
          note: contributionMode === 'deposit' ? form.note.trim() || null : null,
        } satisfies AddContributionRequest),
      });

      const goalName = selectedGoal?.name ?? 'goal';
      setSuccess(
        contributionMode === 'withdraw'
          ? `Withdrawal recorded for ${goalName}.`
          : `Contribution added to ${goalName}.`,
      );
      // Reset the form but preserve the selected goal so the user can quickly add another entry
      setForm((current) => ({
        ...initialForm,
        goalId: current.goalId,
        date: today(),
      }));
      await loadGoals();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to add contribution');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="loading-center">
        {/* biome-ignore lint/a11y/useSemanticElements: Bootstrap spinner requires role=status */}
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="page-header">
        <h1 className="page-title">Savings Goals</h1>
        <p className="page-subtitle">Create goals and track deposits or withdrawals.</p>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="row g-4">
        <SavingsFormsSection
          goalForm={goalForm}
          contributionForm={form}
          contributionMode={contributionMode}
          goals={goals}
          selectableGoals={selectableGoals}
          selectedGoal={selectedGoal}
          contributionLimit={contributionLimit}
          minimumGoalDeadline={minimumGoalDeadline}
          creatingGoal={creatingGoal}
          submitting={submitting}
          amountInputRef={amountInputRef}
          formatCurrency={fmt}
          onCreateGoal={handleCreateGoal}
          onSubmitContribution={handleSubmit}
          onUpdateGoalForm={updateGoalForm}
          onUpdateContributionForm={updateForm}
          onUpdateContributionGoal={updateContributionGoal}
          onUpdateContributionAmount={updateContributionAmount}
          onSelectContributionMode={selectContributionMode}
        />
        <GoalProgressSection
          goals={goals}
          formatCurrency={fmt}
          formatGoalStatus={formatGoalStatus}
          isPausedGoal={isPausedGoal}
          canContributeToGoal={canContributeToGoal}
          onPrepareWithdrawal={prepareWithdrawal}
        />
      </div>
    </div>
  );
}
