import { apiFetch } from '@/api/client';
import { MonthlyReportSchema } from '@/api/schemas';
import type { MonthlyReport } from '@/api/types';
import { useCurrency } from '@/contexts/CurrencyContext';
import { useCurrencyFormatter } from '@/hooks/useCurrencyFormatter';
import { exportReportCsv } from '@/modules/reports/utils/exportCsv';
import { exportReportPdf } from '@/modules/reports/utils/exportPdf';
import { monthLabel } from '@/modules/reports/utils/reportUtils';
import { useState } from 'react';

const now = new Date();
const defaultMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;

function ChangeTag({ value }: { value: number }) {
  const fmt = useCurrencyFormatter();
  const positive = value >= 0;
  return (
    <span className={`badge ${positive ? 'bg-success' : 'bg-danger'}`}>
      {positive ? '+' : ''}
      {fmt(value)}
    </span>
  );
}

export default function ReportsPage() {
  const fmt = useCurrencyFormatter();
  const { currency } = useCurrency();

  const [selectedMonth, setSelectedMonth] = useState(defaultMonth);
  const [report, setReport] = useState<MonthlyReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadReport = async () => {
    const [year, month] = selectedMonth.split('-').map(Number);
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch(
        `/api/reports/monthly?year=${year}&month=${month}`,
        MonthlyReportSchema,
      );
      setReport(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load report.');
    } finally {
      setLoading(false);
    }
  };

  const expenseEntries = Object.entries(report?.expensesByCategory ?? {}).sort(
    ([, a], [, b]) => b - a,
  );
  const incomeEntries = Object.entries(report?.incomeByCategory ?? {}).sort(
    ([, a], [, b]) => b - a,
  );
  const totalExpenses = report?.totalExpenses ?? 0;

  return (
    <div className="container-fluid py-4">
      <div className="d-flex align-items-center justify-content-between mb-4 flex-wrap gap-2">
        <h2 className="mb-0">Monthly Report</h2>
        {report && (
          <div className="d-flex gap-2">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              onClick={() => exportReportCsv(report)}
            >
              Export CSV
            </button>
            <button
              type="button"
              className="btn btn-outline-primary btn-sm"
              onClick={() => exportReportPdf(report, currency)}
            >
              Export PDF
            </button>
          </div>
        )}
      </div>

      <div className="card mb-4">
        <div className="card-body">
          <div className="d-flex align-items-end gap-3 flex-wrap">
            <div>
              <label htmlFor="month-picker" className="form-label mb-1 fw-semibold">
                Select Month
              </label>
              <input
                id="month-picker"
                type="month"
                className="form-control"
                value={selectedMonth}
                onChange={(e) => {
                  setSelectedMonth(e.target.value);
                  setReport(null);
                }}
              />
            </div>
            <button
              type="button"
              className="btn btn-primary"
              onClick={loadReport}
              disabled={loading || !selectedMonth}
            >
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" role="status" />
                  Loading…
                </>
              ) : (
                'Load Report'
              )}
            </button>
          </div>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {report && (
        <>
          <h5 className="text-muted mb-3">{monthLabel(report.year ?? 0, report.month ?? 0)}</h5>

          {/* Summary cards */}
          <div className="row g-3 mb-4">
            <div className="col-sm-4">
              <div className="card h-100">
                <div className="card-body">
                  <div className="text-muted small">Total Income</div>
                  <div className="fs-4 fw-bold text-success">{fmt(report.totalIncome ?? 0)}</div>
                </div>
              </div>
            </div>
            <div className="col-sm-4">
              <div className="card h-100">
                <div className="card-body">
                  <div className="text-muted small">Total Expenses</div>
                  <div className="fs-4 fw-bold text-danger">{fmt(report.totalExpenses ?? 0)}</div>
                </div>
              </div>
            </div>
            <div className="col-sm-4">
              <div className="card h-100">
                <div className="card-body">
                  <div className="text-muted small">Net Savings</div>
                  <div
                    className={`fs-4 fw-bold ${(report.netSavings ?? 0) >= 0 ? 'text-success' : 'text-danger'}`}
                  >
                    {fmt(report.netSavings ?? 0)}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="row g-4 mb-4">
            {/* Expenses by category */}
            <div className="col-lg-6">
              <div className="card h-100">
                <div className="card-header fw-semibold">Expenses by Category</div>
                <div className="card-body p-0">
                  {expenseEntries.length === 0 ? (
                    <p className="text-muted p-3 mb-0">No expenses this month.</p>
                  ) : (
                    <table className="table table-sm mb-0">
                      <thead className="table-light">
                        <tr>
                          <th>Category</th>
                          <th className="text-end">Amount</th>
                          <th className="text-end" style={{ width: 80 }}>
                            Share
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {expenseEntries.map(([cat, amt]) => (
                          <tr key={cat}>
                            <td>{cat}</td>
                            <td className="text-end">{fmt(amt)}</td>
                            <td className="text-end text-muted small">
                              {totalExpenses > 0
                                ? `${Math.round((amt / totalExpenses) * 100)}%`
                                : '—'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </div>
            </div>

            {/* Income by category */}
            <div className="col-lg-6">
              <div className="card h-100">
                <div className="card-header fw-semibold">Income by Category</div>
                <div className="card-body p-0">
                  {incomeEntries.length === 0 ? (
                    <p className="text-muted p-3 mb-0">No income this month.</p>
                  ) : (
                    <table className="table table-sm mb-0">
                      <thead className="table-light">
                        <tr>
                          <th>Category</th>
                          <th className="text-end">Amount</th>
                        </tr>
                      </thead>
                      <tbody>
                        {incomeEntries.map(([cat, amt]) => (
                          <tr key={cat}>
                            <td>{cat}</td>
                            <td className="text-end">{fmt(amt)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </div>
            </div>
          </div>

          <div className="row g-4">
            {/* Savings contributions */}
            <div className="col-lg-6">
              <div className="card h-100">
                <div className="card-header fw-semibold">Savings Goal Contributions</div>
                <div className="card-body p-0">
                  {(report.savingsContributions ?? []).length === 0 ? (
                    <p className="text-muted p-3 mb-0">No savings contributions this month.</p>
                  ) : (
                    <table className="table table-sm mb-0">
                      <thead className="table-light">
                        <tr>
                          <th>Goal</th>
                          <th className="text-end">Deposited</th>
                          <th className="text-end">Withdrawn</th>
                          <th className="text-end">Net</th>
                        </tr>
                      </thead>
                      <tbody>
                        {(report.savingsContributions ?? []).map((c) => (
                          <tr key={c.goalId}>
                            <td>{c.goalName}</td>
                            <td className="text-end text-success">{fmt(c.totalDeposited ?? 0)}</td>
                            <td className="text-end text-danger">
                              {(c.totalWithdrawn ?? 0) > 0 ? fmt(c.totalWithdrawn ?? 0) : '—'}
                            </td>
                            <td className="text-end fw-semibold">
                              <ChangeTag value={c.netContribution ?? 0} />
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </div>
            </div>

            {/* Portfolio change */}
            <div className="col-lg-6">
              <div className="card h-100">
                <div className="card-header fw-semibold">Portfolio Change</div>
                <div className="card-body">
                  {!report.portfolioChange ? (
                    <p className="text-muted mb-0">No portfolio data available.</p>
                  ) : (
                    <table className="table table-sm mb-0">
                      <tbody>
                        <tr>
                          <td className="text-muted">Start of month value</td>
                          <td className="text-end fw-semibold">
                            {fmt(report.portfolioChange.startValue ?? 0)}
                          </td>
                        </tr>
                        <tr>
                          <td className="text-muted">End of month value</td>
                          <td className="text-end fw-semibold">
                            {fmt(report.portfolioChange.endValue ?? 0)}
                          </td>
                        </tr>
                        <tr>
                          <td className="text-muted">Change</td>
                          <td className="text-end">
                            <ChangeTag value={report.portfolioChange.change ?? 0} />
                          </td>
                        </tr>
                        <tr>
                          <td className="text-muted">Change %</td>
                          <td className="text-end">
                            <span
                              className={`fw-semibold ${(report.portfolioChange.changePercent ?? 0) >= 0 ? 'text-success' : 'text-danger'}`}
                            >
                              {report.portfolioChange.changePercent ?? 0}%
                            </span>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  )}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
