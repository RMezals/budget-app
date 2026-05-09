import { useEffect, useState } from 'react';
import { apiFetch } from '../../api/client';
import type { DashboardSummary, AdvisorResult } from '../../api/types';
import FormattedTips from './components/FormattedTips';

function fmt(n: number) {
  return n.toLocaleString('en-US', { style: 'currency', currency: 'EUR' });
}

export default function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [advisor, setAdvisor] = useState<AdvisorResult | null>(null);
  const [advisorLoading, setAdvisorLoading] = useState(false);
  const [advisorError, setAdvisorError] = useState<string | null>(null);
  const [selectedGoals, setSelectedGoals] = useState<string[]>(['save_more']);

  useEffect(() => {
    apiFetch<DashboardSummary>('/api/dashboard').then(setSummary).catch(console.error);
  }, []);

  const runAdvisor = async (provider: 'claude' | 'ollama') => {
    setAdvisorLoading(true);
    setAdvisor(null);
    setAdvisorError(null);
    try {
      const result = await apiFetch<AdvisorResult>('/api/advisor/analyse', {
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
        {summary.budgetUsage.length > 0 && (
          <div className="col-lg-6">
            <div className="card border-0 shadow-sm h-100">
              <div className="card-body">
                <h6 className="card-title mb-3">Budget Usage</h6>
                {summary.budgetUsage.map((b) => (
                  <div key={b.category} className="mb-3">
                    <div className="d-flex justify-content-between mb-1">
                      <span className="small">{b.category}</span>
                      <span className="small text-muted">
                        {fmt(b.spent)} / {fmt(b.limit)}
                      </span>
                    </div>
                    <div className="progress" style={{ height: 8 }}>
                      <div
                        className={`progress-bar ${b.usagePercent >= 90 ? 'bg-danger' : b.usagePercent >= 70 ? 'bg-warning' : 'bg-success'}`}
                        style={{ width: `${Math.min(b.usagePercent, 100)}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Savings goals */}
        {summary.activeGoals.length > 0 && (
          <div className="col-lg-6">
            <div className="card border-0 shadow-sm h-100">
              <div className="card-body">
                <h6 className="card-title mb-3">Savings Goals</h6>
                {summary.activeGoals.map((g) => (
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
                ))}
              </div>
            </div>
          </div>
        )}
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
              {[
                { id: 'save_more', label: 'Save More Money' },
                { id: 'reduce_expenses', label: 'Reduce Expenses' },
                { id: 'invest', label: 'Start Investing' },
                { id: 'emergency_fund', label: 'Build Emergency Fund' },
                { id: 'pay_debt', label: 'Pay Off Debt' },
                { id: 'budget_better', label: 'Budget Better' },
              ].map((goal) => (
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
