import { type FormEvent, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiFetch } from '../../api/client';
import {
  GoalContributionSchema,
  SavingsGoalProgressListSchema,
  SavingsGoalSchema,
} from '../../api/schemas';
import type { SavingsGoalProgress } from '../../api/types';
import { useCurrencyFormatter } from '../../hooks/useCurrencyFormatter';

type GoalForm = {
  name: string;
  targetAmount: string;
  deadline: string;
  description: string;
};

type ContributionForm = {
  goalId: string;
  amount: string;
  date: string;
  note: string;
  reason: string;
};

type ContributionMode = 'deposit' | 'withdraw';

const today = () => new Date().toISOString().slice(0, 10);

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

const getSelectableGoalId = (
  goals: SavingsGoalProgress[],
  currentGoalId: string,
  preferredGoalId?: string,
) => {
  const selectableGoals = goals.filter((goal) => !isCompletedGoal(goal));
  const preferredGoal = selectableGoals.find((goal) => goal.id === preferredGoalId);
  const currentGoal = selectableGoals.find((goal) => goal.id === currentGoalId);

  return preferredGoal?.id ?? currentGoal?.id ?? selectableGoals[0]?.id ?? '';
};

export default function SavingsPage() {
  const fmt = useCurrencyFormatter();
  const amountInputRef = useRef<HTMLInputElement>(null);
  const goalDeadlineInputRef = useRef<HTMLInputElement>(null);
  const contributionDateInputRef = useRef<HTMLInputElement>(null);
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
  const selectableGoals = useMemo(() => goals.filter((goal) => !isCompletedGoal(goal)), [goals]);

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

  const selectContributionMode = (mode: ContributionMode) => {
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
    if (!Number.isFinite(amount) || amount <= 0) {
      setError('Enter an amount greater than zero.');
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
        <div className="col-lg-5">
          <div className="card border-0 shadow-sm mb-4">
            <div className="card-body">
              <h6 className="card-title mb-3">New Goal</h6>
              <form onSubmit={handleCreateGoal}>
                <div className="mb-3">
                  <label className="form-label" htmlFor="goal-name">
                    Name
                  </label>
                  <input
                    id="goal-name"
                    className="form-control"
                    type="text"
                    value={goalForm.name}
                    onChange={(event) => updateGoalForm('name', event.target.value)}
                    placeholder="Emergency fund"
                    disabled={creatingGoal}
                    required
                  />
                </div>

                <div className="row g-3">
                  <div className="col-sm-6">
                    <label className="form-label" htmlFor="goal-target">
                      Target
                    </label>
                    <input
                      id="goal-target"
                      className="form-control"
                      type="number"
                      min="10"
                      step="1"
                      value={goalForm.targetAmount}
                      onChange={(event) => updateGoalForm('targetAmount', event.target.value)}
                      placeholder="5000.00"
                      disabled={creatingGoal}
                      required
                    />
                  </div>
                  <div className="col-sm-6">
                    <label className="form-label" htmlFor="goal-deadline">
                      Deadline
                    </label>
                    <div className="input-group">
                      <input
                        id="goal-deadline"
                        ref={goalDeadlineInputRef}
                        className="form-control"
                        type="date"
                        value={goalForm.deadline}
                        onClick={() => openDatePicker(goalDeadlineInputRef.current)}
                        onChange={(event) => updateGoalForm('deadline', event.target.value)}
                        disabled={creatingGoal}
                        required
                      />
                      <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={() => openDatePicker(goalDeadlineInputRef.current)}
                        disabled={creatingGoal}
                        aria-label="Open deadline calendar"
                        title="Open calendar"
                      >
                        <svg
                          aria-hidden="true"
                          width="18"
                          height="18"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        >
                          <path d="M8 2v4" />
                          <path d="M16 2v4" />
                          <rect width="18" height="18" x="3" y="4" rx="2" />
                          <path d="M3 10h18" />
                        </svg>
                      </button>
                    </div>
                  </div>
                </div>

                <div className="mb-3 mt-3">
                  <label className="form-label" htmlFor="goal-description">
                    Description
                  </label>
                  <textarea
                    id="goal-description"
                    className="form-control"
                    rows={3}
                    value={goalForm.description}
                    onChange={(event) => updateGoalForm('description', event.target.value)}
                    placeholder="Optional details"
                    disabled={creatingGoal}
                  />
                </div>

                <button type="submit" className="btn btn-dark w-100" disabled={creatingGoal}>
                  {creatingGoal ? 'Creating...' : 'Create Goal'}
                </button>
              </form>
            </div>
          </div>

          <div className="card border-0 shadow-sm">
            <div className="card-body">
              <h6 className="card-title mb-3">New Contribution</h6>

              {selectableGoals.length === 0 ? (
                <p className="text-muted small mb-0">
                  {goals.length === 0
                    ? 'Create a savings goal before adding contributions.'
                    : 'All savings goals are completed. Create a new goal before adding contributions.'}
                </p>
              ) : (
                <form onSubmit={handleSubmit}>
                  <fieldset className="mb-3">
                    <legend className="visually-hidden">Contribution type</legend>
                    <div className="btn-group w-100">
                      <button
                        type="button"
                        className={`btn ${contributionMode === 'deposit' ? 'btn-primary' : 'btn-outline-primary'}`}
                        onClick={() => selectContributionMode('deposit')}
                        disabled={submitting}
                      >
                        Deposit
                      </button>
                      <button
                        type="button"
                        className={`btn ${contributionMode === 'withdraw' ? 'btn-danger' : 'btn-outline-danger'}`}
                        onClick={() => selectContributionMode('withdraw')}
                        disabled={submitting}
                      >
                        Withdraw
                      </button>
                    </div>
                  </fieldset>

                  <div className="mb-3">
                    <label className="form-label" htmlFor="contribution-goal">
                      Goal
                    </label>
                    <select
                      id="contribution-goal"
                      className="form-select"
                      value={form.goalId}
                      onChange={(event) => updateForm('goalId', event.target.value)}
                      disabled={submitting}
                    >
                      {selectableGoals.map((goal) => (
                        <option key={goal.id} value={goal.id}>
                          {goal.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="row g-3">
                    <div className="col-sm-6">
                      <label className="form-label" htmlFor="contribution-amount">
                        {contributionMode === 'withdraw' ? 'Withdrawal Amount' : 'Deposit Amount'}
                      </label>
                      <input
                        id="contribution-amount"
                        ref={amountInputRef}
                        className="form-control"
                        type="number"
                        min="10"
                        step="1"
                        value={form.amount}
                        onChange={(event) => updateForm('amount', event.target.value)}
                        placeholder="100.00"
                        disabled={submitting}
                        required
                      />
                    </div>
                    <div className="col-sm-6">
                      <label className="form-label" htmlFor="contribution-date">
                        Date
                      </label>
                      <div className="input-group">
                        <input
                          id="contribution-date"
                          ref={contributionDateInputRef}
                          className="form-control"
                          type="date"
                          value={form.date}
                          onClick={() => openDatePicker(contributionDateInputRef.current)}
                          onChange={(event) => updateForm('date', event.target.value)}
                          disabled={submitting}
                          required
                        />
                        <button
                          type="button"
                          className="btn btn-outline-secondary"
                          onClick={() => openDatePicker(contributionDateInputRef.current)}
                          disabled={submitting}
                          aria-label="Open contribution date calendar"
                          title="Open calendar"
                        >
                          <svg
                            aria-hidden="true"
                            width="18"
                            height="18"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="2"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                          >
                            <path d="M8 2v4" />
                            <path d="M16 2v4" />
                            <rect width="18" height="18" x="3" y="4" rx="2" />
                            <path d="M3 10h18" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>

                  <div className="mb-3 mt-3">
                    <label className="form-label" htmlFor="contribution-reason">
                      Reason
                    </label>
                    <input
                      id="contribution-reason"
                      className="form-control"
                      type="text"
                      value={form.reason}
                      onChange={(event) => updateForm('reason', event.target.value)}
                      placeholder={
                        contributionMode === 'withdraw' ? 'Transfer out' : 'Payday transfer'
                      }
                      disabled={submitting}
                    />
                  </div>

                  {/* <div className="mb-3">
                    <label className="form-label" htmlFor="contribution-note">
                      Note
                    </label>
                    <textarea
                      id="contribution-note"
                      className="form-control"
                      rows={3}
                      value={form.note}
                      onChange={(event) => updateForm('note', event.target.value)}
                      placeholder="Optional details"
                      disabled={submitting}
                    />
                  </div> */}

                  <button
                    type="submit"
                    className={`btn w-100 ${contributionMode === 'withdraw' ? 'btn-danger' : 'btn-primary'}`}
                    disabled={submitting}
                  >
                    {submitting
                      ? contributionMode === 'withdraw'
                        ? 'Withdrawing...'
                        : 'Adding...'
                      : contributionMode === 'withdraw'
                        ? 'Withdraw Money'
                        : 'Add Contribution'}
                  </button>
                </form>
              )}
            </div>
          </div>
        </div>

        <div className="col-lg-7">
          <div className="card border-0 shadow-sm">
            <div className="card-body">
              <h6 className="card-title mb-3">Goal Progress</h6>
              {goals.length === 0 ? (
                <p className="text-muted small mb-0">No savings goals found.</p>
              ) : (
                <div className="d-flex flex-column gap-3">
                  {goals.map((goal) => (
                    <div key={goal.id}>
                      <div className="d-flex justify-content-between gap-3 mb-1">
                        <Link
                          className="fw-semibold link-body-emphasis text-decoration-none"
                          to={`/savings/${goal.id}`}
                        >
                          {goal.name}
                        </Link>
                        <span className="text-muted small text-nowrap">
                          {fmt(goal.currentBalance)} / {fmt(goal.targetAmount)}
                        </span>
                      </div>
                      <div className="progress" style={{ height: 8 }}>
                        <div
                          className="progress-bar bg-primary"
                          style={{ width: `${Math.min(goal.percentReached, 100)}%` }}
                        />
                      </div>

                      <div className="d-flex justify-content-between gap-3 mt-1">
                        <span className="text-muted small">{goal.percentReached}% reached</span>
                        <span className="text-muted small">
                          {fmt(goal.amountRemaining)} remaining
                        </span>
                      </div>
                      <div className="mt-2 mb-2 small text-muted">
                        {formatGoalStatus(goal.status)}
                      </div>
                      <div className="d-flex flex-wrap gap-2 mt-2">
                        <Link
                          className="btn btn-outline-primary btn-sm"
                          to={`/savings/${goal.id}`}
                        >
                          View Goal
                        </Link>
                        <button
                          type="button"
                          className="btn btn-outline-danger btn-sm"
                          onClick={() => prepareWithdrawal(goal.id)}
                          disabled={goal.currentBalance <= 0 || isCompletedGoal(goal)}
                        >
                          Withdraw
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
