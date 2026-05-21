import { Link } from 'react-router-dom';
import type { SavingsGoalProgress } from '../../api/types';

type GoalProgressSectionProps = {
  goals: SavingsGoalProgress[];
  formatCurrency: (value: number) => string;
  formatGoalStatus: (status: SavingsGoalProgress['status']) => string;
  isPausedGoal: (goal: SavingsGoalProgress) => boolean;
  canContributeToGoal: (goal: SavingsGoalProgress) => boolean;
  onPrepareWithdrawal: (goalId: string) => void;
};

const getStatusBadgeClass = (status: string) => {
  switch (status) {
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

export default function GoalProgressSection({
  goals,
  formatCurrency,
  formatGoalStatus,
  isPausedGoal,
  canContributeToGoal,
  onPrepareWithdrawal,
}: GoalProgressSectionProps) {
  return (
    <div className="col-lg-7">
      <div className="card border-0 shadow-sm">
        <div className="card-body">
          <h6 className="card-title mb-3">Goal Progress</h6>
          {goals.length === 0 ? (
            <p className="text-muted small mb-0">No savings goals found.</p>
          ) : (
            <div className="d-flex flex-column gap-3">
              {goals.map((goal) => {
                const status = formatGoalStatus(goal.status);
                const isCompleted = status === 'Completed';
                const isGoalPaused = status === 'Paused';
                const itemClassName = [
                  'border rounded-3 p-3',
                  isCompleted ? 'border-success bg-success-subtle' : 'border-light',
                  isPausedGoal(goal) ? 'opacity-50' : '',
                ]
                  .filter(Boolean)
                  .join(' ');

                return (
                  <div key={goal.id} className={itemClassName}>
                    <div className="d-flex justify-content-between gap-3 mb-2">
                      <div className="d-flex align-items-center gap-2 flex-wrap">
                        <Link
                          className="fw-semibold link-body-emphasis text-decoration-none"
                          to={`/savings/${goal.id}`}
                        >
                          {goal.name}
                        </Link>
                        <span className={`badge ${getStatusBadgeClass(status)}`}>{status}</span>
                      </div>
                      <span className="text-muted small text-nowrap">
                        {formatCurrency(goal.currentBalance)} / {formatCurrency(goal.targetAmount)}
                      </span>
                    </div>
                    <div className="progress" style={{ height: 8 }}>
                      <div
                        className={`progress-bar ${isCompleted ? 'bg-success' : 'bg-primary'}`}
                        style={{ width: `${Math.min(goal.percentReached, 100)}%` }}
                      />
                    </div>

                    <div className="d-flex justify-content-between gap-3 mt-2">
                      <span
                        className={
                          isCompleted ? 'text-success small fw-semibold' : 'text-muted small'
                        }
                      >
                        {isCompleted ? 'Target reached' : `${goal.percentReached}% reached`}
                      </span>
                      <span
                        className={
                          isCompleted ? 'text-success small fw-semibold' : 'text-muted small'
                        }
                      >
                        {isCompleted
                          ? 'Completed'
                          : `${formatCurrency(goal.amountRemaining)} remaining`}
                      </span>
                    </div>
                    <div className="small text-muted mt-1">
                      {!isGoalPaused && (
                        <span className='text-muted'>
                          Projected: {formatDate(goal.projectedCompletion)}
                        </span>
                      )}
                    </div>
                    <div className="d-flex flex-wrap gap-2 mt-3">
                      <Link className="btn btn-outline-primary btn-sm" to={`/savings/${goal.id}`}>
                        View Goal
                      </Link>
                      {!isCompleted && (
                        <button
                          type="button"
                          className="btn btn-outline-danger btn-sm"
                          onClick={() => onPrepareWithdrawal(goal.id)}
                          disabled={goal.currentBalance <= 0 || !canContributeToGoal(goal)}
                        >
                          Withdraw
                        </button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
