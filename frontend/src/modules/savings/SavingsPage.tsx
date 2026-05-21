import { type FormEvent, useEffect, useMemo, useRef, useState } from 'react';
import { apiFetch } from '../../api/client';
import {
  GoalContributionSchema,
  SavingsGoalProgressListSchema,
  SavingsGoalSchema,
} from '../../api/schemas';
import type { SavingsGoalProgress } from '../../api/types';
import { useCurrencyFormatter } from '../../hooks/useCurrencyFormatter';
import GoalProgressSection from './GoalProgressSection';
import SavingsFormsSection, {
  type ContributionForm,
  type ContributionMode,
  type GoalForm,
} from './SavingsFormsSection';

const toDateInputValue = (date: Date) => {
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return localDate.toISOString().slice(0, 10);
};

const daysFromToday = (days: number) => {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return toDateInputValue(date);
};

const today = () => daysFromToday(0);

const initialGoalForm: GoalForm = {
  name: '',
  targetAmount: '',
  deadline: '',
  description: '',
};

const initialForm: ContributionForm = {
  goalId: '',
  amount: '',
  date: today(),
  note: '',
  reason: '',
};

type DateInputWithPicker = HTMLInputElement & {
  showPicker?: () => void;
};

const goalStatusLabels = ['Active', 'Completed', 'Paused', 'Abandoned'] as const;

const formatGoalStatus = (status: SavingsGoalProgress['status']) => {
  if (typeof status === 'number') {
    return goalStatusLabels[status] ?? String(status);
  }

  return status;
};

const isCompletedGoal = (goal: SavingsGoalProgress) =>
  formatGoalStatus(goal.status) === 'Completed';

const isPausedGoal = (goal: SavingsGoalProgress) => formatGoalStatus(goal.status) === 'Paused';

const canContributeToGoal = (goal: SavingsGoalProgress) =>
  !isCompletedGoal(goal) && !isPausedGoal(goal);

const getContributionLimit = (goal: SavingsGoalProgress, mode: ContributionMode) =>
  Math.max(mode === 'withdraw' ? goal.currentBalance : goal.amountRemaining, 0);

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

  return limit > 0 ? String(limit) : '';
};

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

export default function SavingsPage() {
  const fmt = useCurrencyFormatter();
  const amountInputRef = useRef<HTMLInputElement>(null);
  const goalDeadlineInputRef = useRef<HTMLInputElement>(null);
  const contributionDateInputRef = useRef<HTMLInputElement>(null);
  const minimumGoalDeadline = daysFromToday(7);
  const [goals, setGoals] = useState<SavingsGoalProgress[]>([]);
  const [goalForm, setGoalForm] = useState<GoalForm>(initialGoalForm);
  const [form, setForm] = useState<ContributionForm>(initialForm);
  const [contributionMode, setContributionMode] = useState<ContributionMode>('deposit');
  const [loading, setLoading] = useState(true);
  const [creatingGoal, setCreatingGoal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const selectedGoal = useMemo(
    () => goals.find((goal) => goal.id === form.goalId) ?? null,
    [form.goalId, goals],
  );
  const selectableGoals = useMemo(() => goals.filter(canContributeToGoal), [goals]);
  const contributionLimit = selectedGoal
    ? getContributionLimit(selectedGoal, contributionMode)
    : null;

  const loadGoals = async (preferredGoalId?: string) => {
    const data = await apiFetch('/api/goals', SavingsGoalProgressListSchema);
    setGoals(data);
    setForm((current) => ({
      ...current,
      goalId: getSelectableGoalId(data, current.goalId, preferredGoalId),
    }));
  };

  useEffect(() => {
    let cancelled = false;

    const fetchGoals = async () => {
      try {
        const data = await apiFetch('/api/goals', SavingsGoalProgressListSchema);
        if (cancelled) return;
        setGoals(data);
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

    return () => {
      cancelled = true;
    };
  }, []);

  const updateGoalForm = (field: keyof GoalForm, value: string) => {
    setGoalForm((current) => ({ ...current, [field]: value }));
    setSuccess(null);
  };

  const updateForm = (field: keyof ContributionForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setSuccess(null);
  };

  const updateContributionGoal = (goalId: string) => {
    const nextGoal = goals.find((goal) => goal.id === goalId) ?? null;
    setForm((current) => ({
      ...current,
      goalId,
      amount: clampContributionAmount(current.amount, nextGoal, contributionMode),
    }));
    setSuccess(null);
  };

  const updateContributionAmount = (value: string) => {
    setForm((current) => ({
      ...current,
      amount: clampContributionAmount(value, selectedGoal, contributionMode),
    }));
    setSuccess(null);
  };

  const selectContributionMode = (mode: ContributionMode) => {
    setForm((current) => ({
      ...current,
      amount: clampContributionAmount(current.amount, selectedGoal, mode),
    }));
    setContributionMode(mode);
    setSuccess(null);
  };

  const openDatePicker = (input: HTMLInputElement | null) => {
    if (!input || input.disabled) return;

    input.focus();
    try {
      (input as DateInputWithPicker).showPicker?.();
    } catch {
      // Some browsers only allow the native picker from specific user gestures.
    }
  };

  const prepareWithdrawal = (goalId: string) => {
    setContributionMode('withdraw');
    setError(null);
    setSuccess(null);
    setForm((current) => ({
      ...current,
      goalId,
      amount: '',
      date: current.date || today(),
      reason: current.reason || 'Withdrawal',
    }));
    amountInputRef.current?.focus();
  };

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
          deadline: new Date(`${goalForm.deadline}T00:00:00`).toISOString(),
          description: goalForm.description.trim() || null,
        }),
      });

      setGoalForm(initialGoalForm);
      setSuccess(`Goal "${goal.name}" created.`);
      await loadGoals(goal.id);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to create goal');
    } finally {
      setCreatingGoal(false);
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const amount = Math.abs(Number(form.amount));
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

    setSubmitting(true);
    try {
      const signedAmount = contributionMode === 'withdraw' ? -amount : amount;
      await apiFetch(`/api/goals/${form.goalId}/contributions`, GoalContributionSchema, {
        method: 'POST',
        body: JSON.stringify({
          amount: signedAmount,
          date: new Date(`${form.date}T00:00:00`).toISOString(),
          // note: form.note.trim() || null,
          reason: form.reason.trim() || null,
        }),
      });

      const goalName = selectedGoal?.name ?? 'goal';
      setSuccess(
        contributionMode === 'withdraw'
          ? `Withdrawal recorded for ${goalName}.`
          : `Contribution added to ${goalName}.`,
      );
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
      <div className="d-flex justify-content-center py-5">
        {/* biome-ignore lint/a11y/useSemanticElements: Bootstrap spinner requires role=status */}
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="text-start">
      <div className="d-flex flex-column flex-md-row justify-content-between gap-2 mb-4">
        <div>
          <h4 className="mb-1">Savings Goals</h4>
          <p className="text-muted small mb-0">Create goals and track deposits or withdrawals.</p>
        </div>
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
          goalDeadlineInputRef={goalDeadlineInputRef}
          contributionDateInputRef={contributionDateInputRef}
          formatCurrency={fmt}
          onCreateGoal={handleCreateGoal}
          onSubmitContribution={handleSubmit}
          onUpdateGoalForm={updateGoalForm}
          onUpdateContributionForm={updateForm}
          onUpdateContributionGoal={updateContributionGoal}
          onUpdateContributionAmount={updateContributionAmount}
          onSelectContributionMode={selectContributionMode}
          onOpenDatePicker={openDatePicker}
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
