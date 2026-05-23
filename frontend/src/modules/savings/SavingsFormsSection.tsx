import type { FormEvent, RefObject } from 'react';
import DatePicker from '@/components/DatePicker';
import type { SavingsGoalProgress } from '../../api/types';

export type GoalForm = {
  name: string;
  targetAmount: string;
  deadline: string;
  description: string;
};

export type ContributionForm = {
  goalId: string;
  amount: string;
  date: string;
  note: string;
  reason: string;
};

export type ContributionMode = 'deposit' | 'withdraw';

type SavingsFormsSectionProps = {
  goalForm: GoalForm;
  contributionForm: ContributionForm;
  contributionMode: ContributionMode;
  goals: SavingsGoalProgress[];
  selectableGoals: SavingsGoalProgress[];
  selectedGoal: SavingsGoalProgress | null;
  contributionLimit: number | null;
  minimumGoalDeadline: string;
  creatingGoal: boolean;
  submitting: boolean;
  amountInputRef: RefObject<HTMLInputElement | null>;
  formatCurrency: (value: number) => string;
  onCreateGoal: (event: FormEvent<HTMLFormElement>) => void;
  onSubmitContribution: (event: FormEvent<HTMLFormElement>) => void;
  onUpdateGoalForm: (field: keyof GoalForm, value: string) => void;
  onUpdateContributionForm: (field: keyof ContributionForm, value: string) => void;
  onUpdateContributionGoal: (goalId: string) => void;
  onUpdateContributionAmount: (value: string) => void;
  onSelectContributionMode: (mode: ContributionMode) => void;
};

export default function SavingsFormsSection({
  goalForm,
  contributionForm,
  contributionMode,
  goals,
  selectableGoals,
  selectedGoal,
  contributionLimit: _contributionLimit,
  minimumGoalDeadline,
  creatingGoal,
  submitting,
  amountInputRef,
  formatCurrency,
  onCreateGoal,
  onSubmitContribution,
  onUpdateGoalForm,
  onUpdateContributionForm,
  onUpdateContributionGoal,
  onUpdateContributionAmount,
  onSelectContributionMode,
}: SavingsFormsSectionProps) {
  return (
    <div className="col-lg-5">
      {/* New Goal */}
      <div className="card mb-4">
        <div className="card-body">
          <h6 className="card-title mb-3">New Goal</h6>
          <form onSubmit={onCreateGoal}>
            <div className="mb-3">
              <label className="form-label" htmlFor="goal-name">Name</label>
              <input
                id="goal-name"
                className="form-control"
                type="text"
                value={goalForm.name}
                onChange={(e) => onUpdateGoalForm('name', e.target.value)}
                placeholder="Emergency fund"
                disabled={creatingGoal}
                required
              />
            </div>

            <div className="row g-3">
              <div className="col-sm-6">
                <label className="form-label" htmlFor="goal-target">Target</label>
                <input
                  id="goal-target"
                  className="form-control"
                  type="number"
                  min="10"
                  step="1"
                  value={goalForm.targetAmount}
                  onChange={(e) => onUpdateGoalForm('targetAmount', e.target.value)}
                  placeholder="5000.00"
                  disabled={creatingGoal}
                  required
                />
              </div>
              <div className="col-sm-6">
                <label className="form-label" htmlFor="goal-deadline">Deadline</label>
                <DatePicker
                  value={goalForm.deadline}
                  onChange={(v) => onUpdateGoalForm('deadline', v)}
                  min={minimumGoalDeadline}
                  placeholder="Select deadline"
                />
              </div>
            </div>

            <div className="mb-3 mt-3">
              <label className="form-label" htmlFor="goal-description">Description</label>
              <textarea
                id="goal-description"
                className="form-control"
                rows={3}
                value={goalForm.description}
                onChange={(e) => onUpdateGoalForm('description', e.target.value)}
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

      {/* New Contribution */}
      <div className="card">
        <div className="card-body">
          <h6 className="card-title mb-3">New Contribution</h6>

          {selectableGoals.length === 0 ? (
            <p className="text-muted small mb-0">
              {goals.length === 0
                ? 'Create a savings goal before adding contributions.'
                : 'All savings goals are completed or paused. Resume a goal before adding contributions.'}
            </p>
          ) : (
            <form onSubmit={onSubmitContribution}>
              <fieldset className="mb-3">
                <legend className="visually-hidden">Contribution type</legend>
                <div className="btn-group w-100">
                  <button
                    type="button"
                    className={`btn ${contributionMode === 'deposit' ? 'btn-primary' : 'btn-outline-secondary'}`}
                    onClick={() => onSelectContributionMode('deposit')}
                    disabled={submitting}
                  >
                    Deposit
                  </button>
                  <button
                    type="button"
                    className={`btn ${contributionMode === 'withdraw' ? 'btn-danger' : 'btn-outline-secondary'}`}
                    onClick={() => onSelectContributionMode('withdraw')}
                    disabled={submitting}
                  >
                    Withdraw
                  </button>
                </div>
              </fieldset>

              <div className="mb-3">
                <label className="form-label" htmlFor="contribution-goal">Goal</label>
                <select
                  id="contribution-goal"
                  className="form-select"
                  value={contributionForm.goalId}
                  onChange={(e) => onUpdateContributionGoal(e.target.value)}
                  disabled={submitting}
                >
                  {selectableGoals.map((goal) => (
                    <option key={goal.id} value={goal.id}>{goal.name}</option>
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
                    min="0.01"
                    step="0.01"
                    value={contributionForm.amount}
                    onChange={(e) => onUpdateContributionAmount(e.target.value)}
                    placeholder="100.00"
                    disabled={submitting}
                    required
                  />
                  {selectedGoal && (
                    <p className="form-text mb-0">
                      {contributionMode === 'withdraw'
                        ? `${formatCurrency(selectedGoal.currentBalance)} available.`
                        : `${formatCurrency(selectedGoal.amountRemaining)} remaining.`}
                    </p>
                  )}
                </div>
                <div className="col-sm-6">
                  <label className="form-label" htmlFor="contribution-date">Date</label>
                  <DatePicker
                    value={contributionForm.date}
                    onChange={(v) => onUpdateContributionForm('date', v)}
                    placeholder="Select date"
                  />
                </div>
              </div>

              <div className="mb-3 mt-3">
                <label className="form-label" htmlFor="contribution-reason">Reason</label>
                <input
                  id="contribution-reason"
                  className="form-control"
                  type="text"
                  value={contributionForm.reason}
                  onChange={(e) => onUpdateContributionForm('reason', e.target.value)}
                  placeholder={contributionMode === 'withdraw' ? 'Transfer out' : 'Payday transfer'}
                  disabled={submitting}
                />
              </div>

              <button
                type="submit"
                className={`btn w-100 ${contributionMode === 'withdraw' ? 'btn-danger' : 'btn-primary'}`}
                disabled={submitting}
              >
                {submitting
                  ? contributionMode === 'withdraw' ? 'Withdrawing...' : 'Adding...'
                  : contributionMode === 'withdraw' ? 'Withdraw Money' : 'Add Contribution'}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
