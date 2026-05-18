import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { apiFetch } from '../../api/client';
import { GoalContributionListSchema, SavingsGoalProgressSchema } from '../../api/schemas';
import type { GoalContribution, SavingsGoalProgress } from '../../api/types';
import { useCurrencyFormatter } from '../../hooks/useCurrencyFormatter';

const goalStatusLabels = ['Active', 'Completed', 'Paused', 'Abandoned'] as const;

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

export default function GoalPage() {
  const { goalId } = useParams<{ goalId: string }>();
  const fmt = useCurrencyFormatter();
  const [goal, setGoal] = useState<SavingsGoalProgress | null>(null);
  const [contributions, setContributions] = useState<GoalContribution[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const loadGoal = async () => {
      if (!goalId) {
        setError('Goal id is missing.');
        setLoading(false);
        return;
      }

      try {
        const [goalData, contributionData] = await Promise.all([
          apiFetch(`/api/goals/${goalId}`, SavingsGoalProgressSchema),
          apiFetch(`/api/goals/${goalId}/contributions`, GoalContributionListSchema),
        ]);

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
  }, [goalId]);

  const totals = useMemo(
    () =>
      contributions.reduce(
        (current, contribution) => ({
          deposits: current.deposits + (contribution.amount > 0 ? contribution.amount : 0),
          withdrawals:
            current.withdrawals + (contribution.amount < 0 ? Math.abs(contribution.amount) : 0),
        }),
        { deposits: 0, withdrawals: 0 },
      ),
    [contributions],
  );

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

  if (error || !goal) {
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

  return (
    <div className="text-start">
      <div className="d-flex flex-column flex-md-row justify-content-between gap-3 mb-4">
        <div>
          <Link className="btn btn-outline-secondary btn-sm mb-3" to="/savings">
            Back to Savings
          </Link>
          <div className="d-flex align-items-center gap-2 flex-wrap">
            <h4 className="mb-0">{goal.name}</h4>
            <span className={`badge ${getStatusBadgeClass(goal.status)}`}>
              {formatGoalStatus(goal.status)}
            </span>
          </div>
          {goal.description && <p className="text-muted small mt-2 mb-0">{goal.description}</p>}
        </div>
      </div>

      <div className="row g-3 mb-4">
        <div className="col-sm-6 col-lg-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <p className="text-muted small mb-1">Current Balance</p>
              <p className="fs-4 fw-bold mb-0">{fmt(goal.currentBalance)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <p className="text-muted small mb-1">Target</p>
              <p className="fs-4 fw-bold mb-0">{fmt(goal.targetAmount)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <p className="text-muted small mb-1">Remaining</p>
              <p className="fs-4 fw-bold mb-0">{fmt(goal.amountRemaining)}</p>
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

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body">
          <div className="d-flex justify-content-between gap-3 mb-2">
            <span className="fw-semibold">Progress</span>
            <span className="text-muted small">{goal.percentReached}% reached</span>
          </div>
          <div className="progress" style={{ height: 10 }}>
            <div
              className="progress-bar bg-primary"
              style={{ width: `${Math.min(goal.percentReached, 100)}%` }}
            />
          </div>
          <div className="d-flex flex-wrap gap-3 mt-3 small text-muted">
            <span>Deposits: {fmt(totals.deposits)}</span>
            <span>Withdrawals: {fmt(totals.withdrawals)}</span>
            <span>Projected: {formatDate(goal.projectedCompletion)}</span>
          </div>
        </div>
      </div>

      <div className="card border-0 shadow-sm">
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
                  </tr>
                </thead>
                <tbody>
                  {contributions.map((contribution) => (
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
                          contribution.amount < 0 ? 'text-danger' : 'text-success'
                        }`}
                      >
                        {contribution.amount < 0 ? '-' : '+'}
                        {fmt(Math.abs(contribution.amount))}
                      </td>
                      <td className="text-end text-nowrap">{fmt(contribution.balanceAfter)}</td>
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
