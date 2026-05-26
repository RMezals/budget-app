import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { apiFetch } from '../../api/client';
import { GoalContributionListSchema, SavingsGoalProgressSchema } from '../../api/schemas';
import type { GoalContribution, SavingsGoalProgress } from '../../api/types';
import { useCurrencyFormatter } from '../../hooks/useCurrencyFormatter';

const goalStatusLabels = ['Active', 'Completed', 'Paused', 'Abandoned'] as const;
type GoalStatusLabel = (typeof goalStatusLabels)[number];

const goalStatusValues: Record<GoalStatusLabel, 0 | 1 | 2 | 3> = {
  Active: 0,
  Completed: 1,
  Paused: 2,
  Abandoned: 3,
};

const formatGoalStatus = (status: SavingsGoalProgress['status']) => {
  if (typeof status === 'number') {
    return goalStatusLabels[status] ?? String(status);
  }

  return status;
};

const getStatusBadgeClass = (status: SavingsGoalProgress['status']) => {
  switch (formatGoalStatus(status)) {
    case 'Completed':
      return 'text-bg-success';
    case 'Paused':
      return 'text-bg-warning';
    case 'Abandoned':
      return 'text-bg-secondary';
    default:
      return 'text-bg-primary';
  }
};

const formatDate = (value?: string | null) => {
  if (!value) return 'Not set';

  const [datePart] = value.split('T');
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) return value;

  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(new Date(year, month - 1, day));
};

const getDateKey = (value?: string | null) => {
  if (!value) return null;

  const [datePart] = value.split('T');
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) return null;

  return [
    year.toString().padStart(4, '0'),
    month.toString().padStart(2, '0'),
    day.toString().padStart(2, '0'),
  ].join('-');
};

const getProjectionTiming = (goal: SavingsGoalProgress) => {
  const deadline = getDateKey(goal.deadline);
  const projectedCompletion = getDateKey(goal.projectedCompletion);

  if (!deadline || !projectedCompletion) return null;

  return projectedCompletion > deadline ? 'late' : 'onTrack';
};

const getRequiredMonthlyContribution = (goal: SavingsGoalProgress) => {
  const deadlineKey = getDateKey(goal.deadline);
  if (!deadlineKey || (goal.amountRemaining ?? 0) <= 0) return null;

  const [year, month, day] = deadlineKey.split('-').map(Number);
  if (!year || !month || !day) return null;

  const today = new Date();
  const todayDate = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const deadlineDate = new Date(year, month - 1, day);
  const millisecondsRemaining = deadlineDate.getTime() - todayDate.getTime();
  if (millisecondsRemaining <= 0) return null;

  const daysRemaining = millisecondsRemaining / (1000 * 60 * 60 * 24);
  const monthsRemaining = Math.max(daysRemaining / 30.4375, 1);

  return (goal.amountRemaining ?? 0) / monthsRemaining;
};

const toDateInputValue = (value?: string | null) => {
  if (!value) return '';

  const [datePart] = value.split('T');
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) return '';

  return [
    year.toString().padStart(4, '0'),
    month.toString().padStart(2, '0'),
    day.toString().padStart(2, '0'),
  ].join('-');
};

const daysFromToday = (days: number) => {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return localDate.toISOString().slice(0, 10);
};

type GoalEditForm = {
  name: string;
  targetAmount: string;
  deadline: string;
  description: string;
};

type ContributionEditForm = {
  amount: string;
  reason: string;
};

const toGoalEditForm = (goal: SavingsGoalProgress): GoalEditForm => ({
  name: goal.name ?? '',
  targetAmount: String(goal.targetAmount ?? 0),
  deadline: toDateInputValue(goal.deadline),
  description: goal.description ?? '',
});

export default function GoalPage() {
  const { goalId } = useParams<{ goalId: string }>();
  const navigate = useNavigate();
  const fmt = useCurrencyFormatter();
  const [goal, setGoal] = useState<SavingsGoalProgress | null>(null);
  const [contributions, setContributions] = useState<GoalContribution[]>([]);
  const [loading, setLoading] = useState(true);
  const [deletingContributionId, setDeletingContributionId] = useState<string | null>(null);
  const [editingContribution, setEditingContribution] = useState<GoalContribution | null>(null);
  const [savingContributionEdit, setSavingContributionEdit] = useState(false);
  const [contributionEditError, setContributionEditError] = useState<string | null>(null);
  const [contributionEditForm, setContributionEditForm] = useState<ContributionEditForm>({
    amount: '',
    reason: '',
  });
  const [updatingStatus, setUpdatingStatus] = useState<GoalStatusLabel | null>(null);
  const [showAbandonModal, setShowAbandonModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [savingGoalEdit, setSavingGoalEdit] = useState(false);
  const [deletingGoal, setDeletingGoal] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [goalEditForm, setGoalEditForm] = useState<GoalEditForm>({
    name: '',
    targetAmount: '',
    deadline: '',
    description: '',
  });
  const [abandoningGoal, setAbandoningGoal] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchGoalDetails = useCallback(async () => {
    if (!goalId) {
      throw new Error('Goal id is missing.');
    }

    const [goalData, contributionData] = await Promise.all([
      apiFetch(`/api/goals/${goalId}`, SavingsGoalProgressSchema),
      apiFetch(`/api/goals/${goalId}/contributions`, GoalContributionListSchema),
    ]);

    return { goalData: goalData as unknown as SavingsGoalProgress, contributionData };
  }, [goalId]);

  useEffect(() => {
    let cancelled = false;

    const loadGoal = async () => {
      if (!goalId) {
        setError('Goal id is missing.');
        setLoading(false);
        return;
      }

      try {
        const { goalData, contributionData } = await fetchGoalDetails();

        if (cancelled) return;
        setGoal(goalData);
        setContributions(contributionData);
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Unable to load goal');
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    loadGoal();

    return () => {
      cancelled = true;
    };
  }, [fetchGoalDetails, goalId]);

  const totals = useMemo(
    () =>
      contributions.reduce(
        (current, contribution) => ({
          deposits:
            current.deposits + ((contribution.amount ?? 0) > 0 ? (contribution.amount ?? 0) : 0),
          withdrawals:
            current.withdrawals +
            ((contribution.amount ?? 0) < 0 ? Math.abs(contribution.amount ?? 0) : 0),
        }),
        { deposits: 0, withdrawals: 0 },
      ),
    [contributions],
  );

  const contributionRows = useMemo(() => {
    let runningBalance = goal?.currentBalance ?? 0;

    return contributions.map((contribution) => {
      const balanceAfter = runningBalance;
      runningBalance -= contribution.amount ?? 0;

      return { contribution, balanceAfter };
    });
  }, [contributions, goal?.currentBalance]);

  const handleStatusChange = async (status: GoalStatusLabel) => {
    if (!goalId || !goal) return;

    setUpdatingStatus(status);
    setError(null);
    try {
      await apiFetch(`/api/goals/${goalId}/status`, {
        method: 'PUT',
        body: JSON.stringify({ status: goalStatusValues[status] }),
      });
      if (status === 'Abandoned') {
        navigate('/savings', { replace: true });
        return;
      }

      setGoal((current) => (current ? { ...current, status: goalStatusValues[status] } : current));
    } catch (e) {
      setError(e instanceof Error ? e.message : `Unable to mark goal as ${status.toLowerCase()}`);
    } finally {
      setUpdatingStatus(null);
    }
  };

  const openEditModal = () => {
    if (!goal) return;
    setGoalEditForm(toGoalEditForm(goal));
    setEditError(null);
    setShowEditModal(true);
  };

  const openContributionEditModal = (contribution: GoalContribution) => {
    setEditingContribution(contribution);
    setContributionEditForm({
      amount: String(Math.abs(contribution.amount ?? 0)),
      reason: contribution.reason ?? '',
    });
    setContributionEditError(null);
  };

  const handleUpdateContribution = async () => {
    if (!goalId || !editingContribution) return;

    const absoluteAmount = Number(contributionEditForm.amount);
    if (!Number.isFinite(absoluteAmount) || absoluteAmount <= 0) {
      setContributionEditError('Enter an amount greater than zero.');
      return;
    }

    const signedAmount = (editingContribution.amount ?? 0) < 0 ? -absoluteAmount : absoluteAmount;

    setSavingContributionEdit(true);
    setContributionEditError(null);
    setError(null);
    try {
      await apiFetch(`/api/goals/${goalId}/contributions/${editingContribution.id}`, {
        method: 'PUT',
        body: JSON.stringify({
          amount: signedAmount,
          reason: contributionEditForm.reason.trim() || null,
        }),
      });

      const { goalData, contributionData } = await fetchGoalDetails();
      setGoal(goalData);
      setContributions(contributionData);
      setEditingContribution(null);
    } catch (e) {
      setContributionEditError(e instanceof Error ? e.message : 'Unable to update contribution');
    } finally {
      setSavingContributionEdit(false);
    }
  };

  const handleUpdateGoal = async () => {
    if (!goalId || !goal) return;

    const name = goalEditForm.name.trim();
    const targetAmount = Number(goalEditForm.targetAmount);
    if (!name) {
      setEditError('Enter a goal name.');
      return;
    }
    if (!Number.isFinite(targetAmount) || targetAmount <= 0) {
      setEditError('Enter a target amount greater than zero.');
      return;
    }
    if (targetAmount < (goal.currentBalance ?? 0)) {
      setEditError(
        `Target amount cannot be less than current balance (${fmt(goal.currentBalance ?? 0)}).`,
      );
      return;
    }
    if (!goalEditForm.deadline) {
      setEditError('Choose a goal deadline.');
      return;
    }

    setSavingGoalEdit(true);
    setEditError(null);
    setError(null);
    try {
      await apiFetch(`/api/goals/${goalId}`, {
        method: 'PUT',
        body: JSON.stringify({
          name,
          targetAmount,
          deadline: new Date(`${goalEditForm.deadline}T00:00:00`).toISOString(),
          description: goalEditForm.description.trim() || null,
        }),
      });
      const { goalData, contributionData } = await fetchGoalDetails();
      setGoal(goalData);
      setContributions(contributionData);
      setShowEditModal(false);
    } catch (e) {
      setEditError(e instanceof Error ? e.message : 'Unable to update goal');
    } finally {
      setSavingGoalEdit(false);
    }
  };

  const handleAbandonGoal = async () => {
    if (!goalId || !goal) return;

    setAbandoningGoal(true);
    setError(null);
    try {
      await apiFetch(`/api/goals/${goalId}/abandon`, {
        method: 'POST',
        body: JSON.stringify({
          date: new Date().toISOString(),
          reason: 'Goal abandoned',
          description:
            (goal.currentBalance ?? 0) > 0
              ? `Withdrew ${fmt(goal.currentBalance ?? 0)} before abandoning ${goal.name}.`
              : `Abandoned ${goal.name}.`,
        }),
      });
      navigate('/savings', { replace: true });
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to abandon goal');
    } finally {
      setAbandoningGoal(false);
    }
  };

  const handleDeleteContribution = async (contribution: GoalContribution) => {
    if (!goalId) return;

    const contributionType = (contribution.amount ?? 0) < 0 ? 'withdrawal' : 'contribution';
    const confirmed = window.confirm(
      `Delete this ${contributionType} of ${fmt(Math.abs(contribution.amount ?? 0))} from ${formatDate(
        contribution.date,
      )}?`,
    );
    if (!confirmed) return;

    setDeletingContributionId(contribution.id ?? null);
    setError(null);
    try {
      await apiFetch(`/api/goals/${goalId}/contributions/${contribution.id}`, { method: 'DELETE' });
      const { goalData, contributionData } = await fetchGoalDetails();
      setGoal(goalData);
      setContributions(contributionData);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to delete contribution');
    } finally {
      setDeletingContributionId(null);
    }
  };

  const handleDeleteGoal = async () => {
    if (!goalId || !goal) return;

    const confirmed = window.confirm(
      `Delete goal "${goal.name}"? This action cannot be undone and will remove its contribution history.`,
    );
    if (!confirmed) return;

    setDeletingGoal(true);
    setError(null);
    try {
      await apiFetch(`/api/goals/${goalId}`, { method: 'DELETE' });
      navigate('/savings', { replace: true });
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to delete goal');
    } finally {
      setDeletingGoal(false);
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

  if (!goal) {
    return (
      <div className="text-start">
        <Link className="btn btn-outline-secondary btn-sm mb-3" to="/savings">
          Back to Savings
        </Link>
        <div className="alert alert-danger" role="alert">
          {error ?? 'Goal not found.'}
        </div>
      </div>
    );
  }

  const goalStatus = formatGoalStatus(goal.status);
  const isGoalCompleted = goalStatus === 'Completed';
  const isGoalPaused = goalStatus === 'Paused';
  const isGoalAbandoned = goalStatus === 'Abandoned';
  const pausedGoalContentClass = isGoalPaused ? 'opacity-50' : undefined;
  const projectionTiming = isGoalPaused || isGoalCompleted ? null : getProjectionTiming(goal);
  const projectionIsLate = projectionTiming === 'late';
  const projectionBadgeClass = projectionIsLate ? 'text-bg-danger' : 'text-bg-success';
  const projectionTextClass = projectionIsLate ? 'text-danger' : 'text-success';
  const requiredMonthlyContribution =
    isGoalCompleted || isGoalPaused ? null : getRequiredMonthlyContribution(goal);
  const progressBarClass = isGoalCompleted
    ? 'bg-success'
    : projectionTiming === null
      ? 'bg-primary'
      : projectionIsLate
        ? 'bg-danger'
        : 'bg-success';

  return (
    <div className="text-start">
      <div className="d-flex flex-column flex-md-row justify-content-between gap-3 mb-4">
        <div>
          <Link className="btn btn-outline-secondary btn-sm mb-3" to="/savings">
            Back to Savings
          </Link>
          <div className={pausedGoalContentClass}>
            <div className="d-flex align-items-center gap-2 flex-wrap">
              <h4 className="mb-0">{goal.name}</h4>
              <span className={`badge ${getStatusBadgeClass(goal.status)}`}>
                {formatGoalStatus(goal.status)}
              </span>
            </div>
            {goal.description && <p className="text-muted small mt-2 mb-0">{goal.description}</p>}
          </div>
        </div>
        <div className="d-flex flex-wrap gap-2 align-self-md-start justify-content-md-end">
          {!isGoalAbandoned && (
            <button
              type="button"
              className="btn btn-outline-dark btn-sm"
              onClick={openEditModal}
              disabled={
                deletingContributionId !== null ||
                updatingStatus !== null ||
                abandoningGoal ||
                savingGoalEdit ||
                deletingGoal
              }
            >
              Edit Goal
            </button>
          )}
          {!isGoalCompleted && !isGoalAbandoned && (
            <>
              {isGoalPaused ? (
                <button
                  type="button"
                  className="btn btn-outline-primary btn-sm"
                  onClick={() => handleStatusChange('Active')}
                  disabled={
                    deletingContributionId !== null ||
                    updatingStatus !== null ||
                    abandoningGoal ||
                    deletingGoal
                  }
                >
                  {updatingStatus === 'Active' ? 'Resuming...' : 'Resume Goal'}
                </button>
              ) : (
                <button
                  type="button"
                  className="btn btn-outline-warning btn-sm"
                  onClick={() => handleStatusChange('Paused')}
                  disabled={
                    deletingContributionId !== null ||
                    updatingStatus !== null ||
                    abandoningGoal ||
                    deletingGoal
                  }
                >
                  {updatingStatus === 'Paused' ? 'Pausing...' : 'Mark Paused'}
                </button>
              )}
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                onClick={() => setShowAbandonModal(true)}
                disabled={
                  deletingContributionId !== null ||
                  updatingStatus !== null ||
                  abandoningGoal ||
                  deletingGoal
                }
              >
                {abandoningGoal ? 'Abandoning...' : 'Mark Abandoned'}
              </button>
            </>
          )}
          <button
            type="button"
            className="btn btn-danger btn-sm"
            onClick={handleDeleteGoal}
            disabled={
              deletingContributionId !== null ||
              updatingStatus !== null ||
              abandoningGoal ||
              savingGoalEdit ||
              deletingGoal
            }
          >
            {deletingGoal ? 'Deleting...' : 'Delete Goal'}
          </button>
        </div>
      </div>

      {showEditModal && (
        <>
          <dialog
            className="modal show d-block"
            open
            aria-labelledby="edit-goal-title"
            style={{ border: 0, padding: 0, background: 'transparent' }}
            onCancel={(event) => {
              if (savingGoalEdit) {
                event.preventDefault();
                return;
              }

              setShowEditModal(false);
            }}
          >
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title" id="edit-goal-title">
                    Edit goal
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="Close"
                    onClick={() => {
                      setEditError(null);
                      setShowEditModal(false);
                    }}
                    disabled={savingGoalEdit}
                  />
                </div>
                <div className="modal-body">
                  {editError && (
                    <div className="alert alert-danger py-2 mb-3" role="alert">
                      {editError}
                    </div>
                  )}
                  <div className="mb-3">
                    <label className="form-label" htmlFor="edit-goal-name">
                      Name
                    </label>
                    <input
                      id="edit-goal-name"
                      className="form-control"
                      type="text"
                      value={goalEditForm.name}
                      onChange={(event) =>
                        setGoalEditForm((current) => ({ ...current, name: event.target.value }))
                      }
                      disabled={savingGoalEdit}
                      required
                    />
                  </div>
                  <div className="row g-3">
                    <div className="col-sm-6">
                      <label className="form-label" htmlFor="edit-goal-target">
                        Target
                      </label>
                      <input
                        id="edit-goal-target"
                        className="form-control"
                        type="number"
                        min="10"
                        step="1"
                        value={goalEditForm.targetAmount}
                        onChange={(event) =>
                          setGoalEditForm((current) => ({
                            ...current,
                            targetAmount: event.target.value,
                          }))
                        }
                        disabled={savingGoalEdit}
                        required
                      />
                    </div>
                    <div className="col-sm-6">
                      <label className="form-label" htmlFor="edit-goal-deadline">
                        Deadline
                      </label>
                      <input
                        id="edit-goal-deadline"
                        className="form-control"
                        type="date"
                        min={daysFromToday(0)}
                        value={goalEditForm.deadline}
                        onChange={(event) =>
                          setGoalEditForm((current) => ({
                            ...current,
                            deadline: event.target.value,
                          }))
                        }
                        disabled={savingGoalEdit}
                        required
                      />
                    </div>
                  </div>
                  <div className="mt-3">
                    <label className="form-label" htmlFor="edit-goal-description">
                      Description
                    </label>
                    <textarea
                      id="edit-goal-description"
                      className="form-control"
                      rows={3}
                      value={goalEditForm.description}
                      onChange={(event) =>
                        setGoalEditForm((current) => ({
                          ...current,
                          description: event.target.value,
                        }))
                      }
                      disabled={savingGoalEdit}
                    />
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => {
                      setEditError(null);
                      setShowEditModal(false);
                    }}
                    disabled={savingGoalEdit}
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    className="btn btn-primary"
                    onClick={handleUpdateGoal}
                    disabled={savingGoalEdit}
                  >
                    {savingGoalEdit ? 'Saving...' : 'Save Changes'}
                  </button>
                </div>
              </div>
            </div>
          </dialog>
          <div className="modal-backdrop show" />
        </>
      )}

      {editingContribution && (
        <>
          <dialog
            className="modal show d-block"
            open
            aria-labelledby="edit-contribution-title"
            style={{ border: 0, padding: 0, background: 'transparent' }}
            onCancel={(event) => {
              if (savingContributionEdit) {
                event.preventDefault();
                return;
              }

              setEditingContribution(null);
            }}
          >
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title" id="edit-contribution-title">
                    Edit {(editingContribution.amount ?? 0) < 0 ? 'withdrawal' : 'contribution'}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="Close"
                    onClick={() => {
                      setContributionEditError(null);
                      setEditingContribution(null);
                    }}
                    disabled={savingContributionEdit}
                  />
                </div>
                <div className="modal-body">
                  {contributionEditError && (
                    <div className="alert alert-danger py-2 mb-3" role="alert">
                      {contributionEditError}
                    </div>
                  )}
                  <div className="row g-3">
                    <div className="col-sm-6">
                      <label className="form-label" htmlFor="edit-contribution-amount">
                        Amount
                      </label>
                      <input
                        id="edit-contribution-amount"
                        className="form-control"
                        type="number"
                        min="0.01"
                        step="0.01"
                        value={contributionEditForm.amount}
                        onChange={(event) =>
                          setContributionEditForm((current) => ({
                            ...current,
                            amount: event.target.value,
                          }))
                        }
                        disabled={savingContributionEdit}
                        required
                      />
                    </div>
                    <div className="col-sm-6">
                      <label className="form-label" htmlFor="edit-contribution-date">
                        Date
                      </label>
                      <input
                        id="edit-contribution-date"
                        className="form-control"
                        type="text"
                        value={formatDate(editingContribution.date)}
                        disabled
                        readOnly
                      />
                    </div>
                  </div>
                  <div className="mt-3">
                    <label className="form-label" htmlFor="edit-contribution-reason">
                      Reason
                    </label>
                    <input
                      id="edit-contribution-reason"
                      className="form-control"
                      type="text"
                      value={contributionEditForm.reason}
                      onChange={(event) =>
                        setContributionEditForm((current) => ({
                          ...current,
                          reason: event.target.value,
                        }))
                      }
                      disabled={savingContributionEdit}
                    />
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => {
                      setContributionEditError(null);
                      setEditingContribution(null);
                    }}
                    disabled={savingContributionEdit}
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    className="btn btn-primary"
                    onClick={handleUpdateContribution}
                    disabled={savingContributionEdit}
                  >
                    {savingContributionEdit ? 'Saving...' : 'Save Changes'}
                  </button>
                </div>
              </div>
            </div>
          </dialog>
          <div className="modal-backdrop show" />
        </>
      )}

      {showAbandonModal && (
        <>
          <dialog
            className="modal show d-block"
            open
            aria-labelledby="abandon-goal-title"
            style={{ border: 0, padding: 0, background: 'transparent' }}
            onCancel={(event) => {
              if (abandoningGoal) {
                event.preventDefault();
                return;
              }

              setShowAbandonModal(false);
            }}
          >
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title" id="abandon-goal-title">
                    Abandon goal?
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="Close"
                    onClick={() => setShowAbandonModal(false)}
                    disabled={abandoningGoal}
                  />
                </div>
                <div className="modal-body">
                  <p className="mb-2">
                    This will withdraw {fmt(goal.currentBalance ?? 0)} from {goal.name} and mark the
                    goal as abandoned.
                  </p>
                  <p className="text-muted small mb-0">
                    The withdrawal will appear in the contribution history.
                  </p>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => setShowAbandonModal(false)}
                    disabled={abandoningGoal}
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
                    onClick={handleAbandonGoal}
                    disabled={abandoningGoal}
                  >
                    {abandoningGoal ? 'Abandoning...' : 'Withdraw saved amount and abandon goal'}
                  </button>
                </div>
              </div>
            </div>
          </dialog>
          <div className="modal-backdrop show" />
        </>
      )}

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      <div className={`row g-3 mb-4 ${pausedGoalContentClass ?? ''}`}>
        <div className="col-sm-6 col-lg-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <p className="text-muted small mb-1">Current Balance</p>
              <p className="fs-4 fw-bold mb-0">{fmt(goal.currentBalance ?? 0)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <p className="text-muted small mb-1">Target</p>
              <p className="fs-4 fw-bold mb-0">{fmt(goal.targetAmount ?? 0)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <p className="text-muted small mb-1">Remaining</p>
              <p className="fs-4 fw-bold mb-0">{fmt(goal.amountRemaining ?? 0)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <p className="text-muted small mb-1">Deadline</p>
              <p className="fs-5 fw-semibold mb-0">{formatDate(goal.deadline)}</p>
            </div>
          </div>
        </div>
      </div>

      <div className={`card border-0 shadow-sm mb-4 ${pausedGoalContentClass ?? ''}`}>
        <div className="card-body">
          <div className="d-flex justify-content-between gap-3 mb-2">
            <span className="fw-semibold">Progress</span>
            <span
              className={isGoalCompleted ? 'text-success small fw-semibold' : 'text-muted small'}
            >
              {isGoalCompleted ? 'Target reached' : `${goal.percentReached}% reached`}
            </span>
          </div>
          <div className="progress" style={{ height: 10 }}>
            <div
              className={`progress-bar ${progressBarClass}`}
              style={{ width: `${Math.min(goal.percentReached ?? 0, 100)}%` }}
            />
          </div>
          <div className="d-flex flex-wrap align-items-center gap-3 mt-3 small">
            <span className="text-muted">Deposits: {fmt(totals.deposits)}</span>
            <span className="text-muted">Withdrawals: {fmt(totals.withdrawals)}</span>
            {isGoalCompleted && <span className="text-success fw-semibold">Completed</span>}
            {!isGoalCompleted && !isGoalPaused && (
              <span className={projectionTiming ? projectionTextClass : 'text-muted'}>
                Projected: {formatDate(goal.projectedCompletion)}
              </span>
            )}
            {!isGoalCompleted && !isGoalPaused && requiredMonthlyContribution !== null && (
              <span className="text-muted">
                Required monthly: {fmt(requiredMonthlyContribution)}
              </span>
            )}
            {projectionTiming && (
              <span className={`badge ${projectionBadgeClass}`}>
                {projectionIsLate ? 'After deadline' : 'On track'}
              </span>
            )}
          </div>
        </div>
      </div>

      <div className={`card border-0 shadow-sm ${pausedGoalContentClass ?? ''}`}>
        <div className="card-body">
          <div className="d-flex justify-content-between align-items-center gap-3 mb-3">
            <h6 className="card-title mb-0">Contributions</h6>
            <span className="text-muted small">{contributions.length} total</span>
          </div>

          {contributions.length === 0 ? (
            <p className="text-muted small mb-0">No contributions recorded for this goal yet.</p>
          ) : (
            <div className="table-responsive">
              <table className="table align-middle mb-0">
                <thead>
                  <tr>
                    <th scope="col">Date</th>
                    <th scope="col">Reason</th>
                    <th scope="col" className="text-end">
                      Amount
                    </th>
                    <th scope="col" className="text-end">
                      Balance
                    </th>
                    <th scope="col" className="text-end">
                      Action
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {contributionRows.map(({ contribution, balanceAfter }) => (
                    <tr key={contribution.id}>
                      <td className="text-nowrap">{formatDate(contribution.date)}</td>
                      <td>
                        <div>{contribution.reason || 'Contribution'}</div>
                        {contribution.description &&
                          contribution.description !== contribution.reason && (
                            <div className="text-muted small">{contribution.description}</div>
                          )}
                      </td>
                      <td
                        className={`text-end fw-semibold ${
                          (contribution.amount ?? 0) < 0 ? 'text-danger' : 'text-success'
                        }`}
                      >
                        {(contribution.amount ?? 0) < 0 ? '-' : '+'}
                        {fmt(Math.abs(contribution.amount ?? 0))}
                      </td>
                      <td className="text-end text-nowrap">{fmt(balanceAfter)}</td>
                      <td className="text-end">
                        <div className="d-inline-flex gap-2">
                          <button
                            type="button"
                            className="btn btn-outline-dark btn-sm"
                            onClick={() => openContributionEditModal(contribution)}
                            disabled={
                              deletingContributionId !== null ||
                              updatingStatus !== null ||
                              abandoningGoal ||
                              deletingGoal ||
                              savingContributionEdit
                            }
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm"
                            onClick={() => handleDeleteContribution(contribution)}
                            disabled={
                              deletingContributionId !== null ||
                              updatingStatus !== null ||
                              abandoningGoal ||
                              deletingGoal ||
                              savingContributionEdit
                            }
                          >
                            {deletingContributionId === contribution.id ? 'Deleting...' : 'Delete'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
