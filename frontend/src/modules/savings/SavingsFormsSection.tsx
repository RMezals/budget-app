import type { FormEvent, RefObject } from 'react';
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
  goalDeadlineInputRef: RefObject<HTMLInputElement | null>;
  contributionDateInputRef: RefObject<HTMLInputElement | null>;
  formatCurrency: (value: number) => string;
  onCreateGoal: (event: FormEvent<HTMLFormElement>) => void;
  onSubmitContribution: (event: FormEvent<HTMLFormElement>) => void;
  onUpdateGoalForm: (field: keyof GoalForm, value: string) => void;
  onUpdateContributionForm: (field: keyof ContributionForm, value: string) => void;
  onUpdateContributionGoal: (goalId: string) => void;
  onUpdateContributionAmount: (value: string) => void;
  onSelectContributionMode: (mode: ContributionMode) => void;
  onOpenDatePicker: (input: HTMLInputElement | null) => void;
};

function CalendarIcon() {
  return (
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
  );
}

export default function SavingsFormsSection({
  goalForm,
  contributionForm,
  contributionMode,
  goals,
  selectableGoals,
  selectedGoal,
  contributionLimit,
  minimumGoalDeadline,
  creatingGoal,
  submitting,
  amountInputRef,
  goalDeadlineInputRef,
  contributionDateInputRef,
  formatCurrency,
  onCreateGoal,
  onSubmitContribution,
  onUpdateGoalForm,
  onUpdateContributionForm,
  onUpdateContributionGoal,
  onUpdateContributionAmount,
  onSelectContributionMode,
  onOpenDatePicker,
}: SavingsFormsSectionProps) {
  return (
    <div className="col-lg-5">
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body">
          <h6 className="card-title mb-3">New Goal</h6>
          <form onSubmit={onCreateGoal}>
            <div className="mb-3">
              <label className="form-label" htmlFor="goal-name">
                Name
              </label>
              <input
                id="goal-name"
                className="form-control"
                type="text"
                value={goalForm.name}
                onChange={(event) => onUpdateGoalForm('name', event.target.value)}
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
                  onChange={(event) => onUpdateGoalForm('targetAmount', event.target.value)}
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
                    min={minimumGoalDeadline}
                    value={goalForm.deadline}
                    onClick={() => onOpenDatePicker(goalDeadlineInputRef.current)}
                    onChange={(event) => onUpdateGoalForm('deadline', event.target.value)}
                    disabled={creatingGoal}
                    required
                  />
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => onOpenDatePicker(goalDeadlineInputRef.current)}
                    disabled={creatingGoal}
                    aria-label="Open deadline calendar"
                    title="Open calendar"
                  >
                    <CalendarIcon />
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
                onChange={(event) => onUpdateGoalForm('description', event.target.value)}
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
                : 'All savings goals are completed or paused. Resume a goal before adding contributions.'}
            </p>
          ) : (
            <form onSubmit={onSubmitContribution}>
              <fieldset className="mb-3">
                <legend className="visually-hidden">Contribution type</legend>
                <div className="btn-group w-100">
                  <button
                    type="button"
                    className={`btn ${contributionMode === 'deposit' ? 'btn-primary' : 'btn-outline-primary'}`}
                    onClick={() => onSelectContributionMode('deposit')}
                    disabled={submitting}
                  >
                    Deposit
                  </button>
                  <button
                    type="button"
                    className={`btn ${contributionMode === 'withdraw' ? 'btn-danger' : 'btn-outline-danger'}`}
                    onClick={() => onSelectContributionMode('withdraw')}
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
                  value={contributionForm.goalId}
                  onChange={(event) => onUpdateContributionGoal(event.target.value)}
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
                    min="0.01"
                    max={contributionLimit ?? undefined}
                    step="0.01"
                    value={contributionForm.amount}
                    onChange={(event) => onUpdateContributionAmount(event.target.value)}
                    placeholder="100.00"
                    disabled={submitting}
                    required
                  />
                  {selectedGoal && (
                    <p className="form-text mb-0">
                      {contributionMode === 'withdraw'
                        ? `${formatCurrency(selectedGoal.currentBalance)} available to withdraw.`
                        : `${formatCurrency(selectedGoal.amountRemaining)} remaining for this goal.`}
                    </p>
                  )}
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
                      value={contributionForm.date}
                      onClick={() => onOpenDatePicker(contributionDateInputRef.current)}
                      onChange={(event) => onUpdateContributionForm('date', event.target.value)}
                      disabled={submitting}
                      required
                    />
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      onClick={() => onOpenDatePicker(contributionDateInputRef.current)}
                      disabled={submitting}
                      aria-label="Open contribution date calendar"
                      title="Open calendar"
                    >
                      <CalendarIcon />
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
                  value={contributionForm.reason}
                  onChange={(event) => onUpdateContributionForm('reason', event.target.value)}
                  placeholder={contributionMode === 'withdraw' ? 'Transfer out' : 'Payday transfer'}
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
                  value={contributionForm.note}
                  onChange={(event) => onUpdateContributionForm('note', event.target.value)}
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
  );
}
