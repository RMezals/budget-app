import { useEffect, useState } from 'react';
import { apiFetch } from '../../api/client';
import { AdvisorResultSchema, DashboardSummarySchema } from '../../api/schemas';
import type { AdvisorResult, DashboardSummary } from '../../api/types';
import { useCurrencyFormatter } from '../../hooks/useCurrencyFormatter';
import EmptyState from './components/EmptyState';
import FormattedTips from './components/FormattedTips';
import { ADVISOR_GOAL_OPTIONS } from './constants/advisorGoals';
import { EMPTY_STATE_MESSAGES } from './constants/emptyStateMessages';
import { getBudgetProgressColor } from './utils/budgetUtils';
import { hasBudgetData, hasSavingsGoals } from './utils/dataChecks';

export default function DashboardPage() {
  const fmt = useCurrencyFormatter();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [advisor, setAdvisor] = useState<AdvisorResult | null>(null);
  const [advisorLoading, setAdvisorLoading] = useState(false);
  const [advisorError, setAdvisorError] = useState<string | null>(null);
  const [selectedGoals, setSelectedGoals] = useState<string[]>(['save_more']);

  useEffect(() => {
    let cancelled = false;
    apiFetch('/api/dashboard', DashboardSummarySchema)
      .then((data) => {
        if (!cancelled) setSummary(data);
      })
      .catch(console.error);
    return () => {
      cancelled = true;
    };
  }, []);

  const runAdvisor = async (provider: 'claude' | 'ollama') => {
    if (selectedGoals.length === 0) {
      setAdvisorError('Please select at least one goal');
      return;
    }

    setAdvisorLoading(true);
    setAdvisor(null);
    setAdvisorError(null);
    try {
      const result = await apiFetch('/api/advisor/analyse', AdvisorResultSchema, {
        method: 'POST',
        body: JSON.stringify({ provider, goals: selectedGoals }),
      });
      setAdvisor(result);
    } catch (e) {
      console.error(e);
      setAdvisorError(e instanceof Error ? e.message : 'An error occurred');
    } finally {
      setAdvisorLoading(false);
    }
  };

  const toggleGoal = (goal: string) => {
    setSelectedGoals((prev) =>
      prev.includes(goal) ? prev.filter((g) => g !== goal) : [...prev, goal],
    );
  };

  if (!summary)
    return (
      <div className="d-flex justify-content-center py-5">
        {/* biome-ignore lint/a11y/useSemanticElements: Bootstrap spinner requires role=status */}
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );

  const netForMonth = summary.monthlyIncome - summary.monthlyExpenses;

  return (
    <div>
      <h4 className="mb-4">Dashboard</h4>

      {/* Overview cards */}
      <div className="row g-3 mb-4">
        <div className="col-sm-6 col-lg-3">
          <div className="card h-100 border-0 shadow-sm">
            <div className="card-body">
              <p className="text-muted small mb-1">Net Worth</p>
              <p className="fs-4 fw-bold mb-0">{fmt(summary.netWorth)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card h-100 border-0 shadow-sm">
            <div className="card-body">
              <p className="text-muted small mb-1">Total Invested</p>
              <p className="fs-4 fw-bold mb-0">{fmt(summary.totalInvested)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card h-100 border-0 shadow-sm">
            <div className="card-body">
              <p className="text-muted small mb-1">Total Saved</p>
              <p className="fs-4 fw-bold mb-0">{fmt(summary.totalSaved)}</p>
            </div>
          </div>
        </div>
        <div className="col-sm-6 col-lg-3">
          <div className="card h-100 border-0 shadow-sm">
            <div className="card-body">
              <p className="text-muted small mb-1">This Month</p>
              <p
                className={`fs-4 fw-bold mb-0 ${netForMonth >= 0 ? 'text-success' : 'text-danger'}`}
              >
                {netForMonth >= 0 ? '+' : ''}
                {fmt(netForMonth)}
              </p>
              <p className="text-muted small mb-0">
                {fmt(summary.monthlyIncome)} in · {fmt(summary.monthlyExpenses)} out
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="row g-4">
        {/* Budget usage */}
        <div className="col-lg-6">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <h6 className="card-title mb-3">Budget Usage</h6>
              {hasBudgetData(summary) ? (
                summary.budgetUsage.map((b) => (
                  <div key={b.category} className="mb-3">
                    <div className="d-flex justify-content-between mb-1">
                      <span className="small">{b.category}</span>
                      <span className="small text-muted">
                        {fmt(b.spent)} / {fmt(b.limit)}
                      </span>
                    </div>
                    <div className="progress" style={{ height: 8 }}>
                      <div
                        className={`progress-bar ${getBudgetProgressColor(b.usagePercent)}`}
                        style={{ width: `${Math.min(b.usagePercent, 100)}%` }}
                      />
                    </div>
                  </div>
                ))
              ) : (
                <EmptyState {...EMPTY_STATE_MESSAGES.BUDGET_USAGE} />
              )}
            </div>
          </div>
        </div>

        {/* Savings goals */}
        <div className="col-lg-6">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <h6 className="card-title mb-3">Savings Goals</h6>
              {hasSavingsGoals(summary) ? (
                summary.activeGoals.map((g) => (
                  <div key={g.goalId} className="mb-3">
                    <div className="d-flex justify-content-between mb-1">
                      <span className="small">{g.name}</span>
                      <span className="small text-muted">
                        {fmt(g.currentAmount)} / {fmt(g.targetAmount)}
                      </span>
                    </div>
                    <div className="progress" style={{ height: 8 }}>
                      <div
                        className="progress-bar bg-primary"
                        style={{ width: `${g.percentReached}%` }}
                      />
                    </div>
                    <p className="text-muted small mb-0 mt-1">{g.percentReached}% reached</p>
                  </div>
                ))
              ) : (
                <EmptyState {...EMPTY_STATE_MESSAGES.SAVINGS_GOALS} />
              )}
            </div>
          </div>
        </div>
      </div>

      {/* AI Advisor */}
      <div className="card border-0 shadow-sm mt-4">
        <div className="card-body">
          <h6 className="card-title mb-1">AI Financial Advisor</h6>
          <p className="text-muted small mb-3">
            Get personalised tips based on your current finances and goals.
          </p>

          {/* Goal Selection */}
          <div className="mb-3">
            <p className="small fw-semibold mb-2">What would you like advice on?</p>
            <div className="d-flex flex-wrap gap-2">
              {ADVISOR_GOAL_OPTIONS.map((goal) => (
                <button
                  key={goal.id}
                  type="button"
                  className={`btn btn-sm ${selectedGoals.includes(goal.id) ? 'btn-primary' : 'btn-outline-secondary'}`}
                  onClick={() => toggleGoal(goal.id)}
                >
                  {goal.label}
                </button>
              ))}
            </div>
          </div>

          <div className="d-flex gap-2 mb-3">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              onClick={() => runAdvisor('ollama')}
              disabled={advisorLoading}
            >
              Get Tips (Free — Ollama)
            </button>
            <button
              type="button"
              className="btn btn-dark btn-sm"
              onClick={() => runAdvisor('claude')}
              disabled={advisorLoading}
            >
              Get Tips (Claude)
            </button>
          </div>
          {advisorLoading && (
            <div className="d-flex align-items-center gap-2 text-muted small">
              <div className="spinner-border spinner-border-sm" role="status" />
              Analysing your finances…
            </div>
          )}
          {advisorError && (
            <div className="alert alert-danger mb-0">
              <p className="mb-0">
                <strong>Error:</strong> {advisorError}
              </p>
            </div>
          )}
          {advisor && <FormattedTips rawTips={advisor.tips} provider={advisor.provider} />}
        </div>
      </div>
    </div>
  );
}
