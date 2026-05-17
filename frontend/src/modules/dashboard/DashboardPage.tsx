import { useEffect, useState } from 'react';
import { apiFetch } from '@/api/client';
import { DashboardSummarySchema } from '@/api/schemas';
import type { DashboardSummary } from '@/api/types';
import { useCurrencyFormatter } from '@/hooks/useCurrencyFormatter';
import EmptyState from '@/modules/dashboard/components/EmptyState';
import FormattedTips from '@/modules/dashboard/components/FormattedTips';
import { ADVISOR_GOAL_OPTIONS } from '@/modules/dashboard/constants/advisorGoals';
import { EMPTY_STATE_MESSAGES } from '@/modules/dashboard/constants/emptyStateMessages';
import { useAdvisor } from '@/modules/dashboard/hooks/useAdvisor';
import { useClaudeApiKey } from '@/modules/dashboard/hooks/useClaudeApiKey';
import { getBudgetProgressColor } from '@/modules/dashboard/utils/budgetUtils';
import { hasBudgetData, hasSavingsGoals } from '@/modules/dashboard/utils/dataChecks';

export default function DashboardPage() {
  const fmt = useCurrencyFormatter();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [selectedGoals, setSelectedGoals] = useState<string[]>(['save_more']);
  const [customQuestion, setCustomQuestion] = useState('');

  // Custom hooks for advisor and API key management
  const apiKeyManager = useClaudeApiKey();
  const {
    advisor,
    loading: advisorLoading,
    error: advisorError,
    runAdvisor,
  } = useAdvisor({
    claudeApiKey: apiKeyManager.apiKey,
    onClaudeKeyRequired: apiKeyManager.openModal,
  });

  // Load dashboard summary on mount
  useEffect(() => {
    let cancelled = false;
    apiFetch('/api/dashboard', DashboardSummarySchema)
      .then((data) => {
        if (!cancelled) setSummary(data);
      })
      .catch((error) => {
        if (!cancelled) console.error(error);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const handleRunAdvisor = async (provider: 'claude' | 'ollama') => {
    const goals = customQuestion.trim() ? [...selectedGoals, customQuestion.trim()] : selectedGoals;

    await runAdvisor(provider, goals);
  };

  const toggleGoal = (goal: string) => {
    setSelectedGoals((prev) =>
      prev.includes(goal) ? prev.filter((g) => g !== goal) : [...prev, goal],
    );
  };

  const handleSaveApiKey = async () => {
    await apiKeyManager.saveApiKey();
    // Auto-run Claude advisor after saving API key
    if (apiKeyManager.tempKey.trim()) {
      const goals = customQuestion.trim()
        ? [...selectedGoals, customQuestion.trim()]
        : selectedGoals;
      await runAdvisor('claude', goals);
    }
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

          {/* Custom Question */}
          <div className="mb-3">
            <label htmlFor="customQuestion" className="form-label small fw-semibold">
              Or ask your own question:
            </label>
            <textarea
              id="customQuestion"
              className="form-control form-control-sm"
              rows={2}
              placeholder="e.g., Should I prioritize paying off my credit card or saving for a house?"
              value={customQuestion}
              onChange={(e) => setCustomQuestion(e.target.value)}
            />
          </div>

          <div className="d-flex gap-2 mb-3 align-items-center flex-wrap">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              onClick={() => handleRunAdvisor('ollama')}
              disabled={advisorLoading}
            >
              Get Tips (Free — Ollama)
            </button>
            <button
              type="button"
              className="btn btn-dark btn-sm"
              onClick={() => handleRunAdvisor('claude')}
              disabled={advisorLoading}
            >
              Get Tips (Claude)
            </button>
            {apiKeyManager.apiKey && (
              <small className="text-muted ms-2">
                API key configured ·{' '}
                <button
                  type="button"
                  className="btn btn-link btn-sm p-0 text-decoration-none"
                  onClick={apiKeyManager.clearApiKey}
                  style={{ fontSize: 'inherit' }}
                >
                  Clear
                </button>
              </small>
            )}
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

      {/* Claude API Key Modal */}
      {apiKeyManager.showModal && (
        <div
          className="modal show d-block"
          style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
          onClick={apiKeyManager.closeModal}
          onKeyDown={(e) => {
            if (e.key === 'Escape') {
              apiKeyManager.closeModal();
            }
          }}
        >
          <div
            className="modal-dialog modal-dialog-centered"
            onClick={(e) => e.stopPropagation()}
            onKeyDown={(e) => e.stopPropagation()}
          >
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Claude API Key Required</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={apiKeyManager.closeModal}
                  aria-label="Close"
                />
              </div>
              <div className="modal-body">
                <p className="text-muted small mb-3">
                  To use Claude AI, you need an API key from Anthropic. Get one at{' '}
                  <a
                    href="https://console.anthropic.com"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-decoration-none"
                  >
                    console.anthropic.com
                  </a>
                </p>
                <div className="alert alert-warning small">
                  <strong>Security Note:</strong> Your API key is encrypted and stored locally in
                  your browser. However, this provides only basic protection. For maximum security,
                  consider alternative approaches for production use.
                </div>
                <div className="mb-3">
                  <label htmlFor="apiKeyInput" className="form-label small fw-semibold">
                    Enter your Claude API Key:
                  </label>
                  <input
                    id="apiKeyInput"
                    type="password"
                    className="form-control"
                    placeholder="sk-ant-..."
                    value={apiKeyManager.tempKey}
                    onChange={(e) => apiKeyManager.setTempKey(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleSaveApiKey()}
                  />
                  <small className="text-muted">
                    Your API key is stored locally in your browser.
                  </small>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={apiKeyManager.closeModal}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn btn-primary btn-sm"
                  onClick={handleSaveApiKey}
                  disabled={!apiKeyManager.tempKey.trim()}
                >
                  Save & Continue
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
