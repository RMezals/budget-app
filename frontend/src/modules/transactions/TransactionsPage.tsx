import DatePicker from '@/components/DatePicker';
import { useEffect, useRef, useState } from 'react';
import { apiFetch } from '../../api/client';
import {
  BudgetListSchema,
  TransactionBudgetUsageListSchema,
  TransactionCategoriesSchema,
  TransactionListSchema,
  TransactionSchema,
} from '../../api/schemas';
import type {
  Budget,
  Transaction,
  TransactionBudgetUsage,
  TransactionCategories,
} from '../../api/types';
import { useCurrencyFormatter } from '../../hooks/useCurrencyFormatter';

// ── helpers ────────────────────────────────────────────────────────────────

const todayStr = () => new Date().toISOString().slice(0, 10);

const currentYear = new Date().getFullYear();
const currentMonth = new Date().getMonth() + 1;

function monthLabel(year: number, month: number) {
  return new Date(year, month - 1, 1).toLocaleString('en-GB', { month: 'long', year: 'numeric' });
}

// ── types ──────────────────────────────────────────────────────────────────

type Tab = 'transactions' | 'budgets';

type TxForm = {
  amount: string;
  date: string;
  category: string;
  description: string;
  isIncome: boolean;
};

const emptyTxForm = (categories: TransactionCategories): TxForm => ({
  amount: '',
  date: todayStr(),
  category: categories.expense[0] ?? '',
  description: '',
  isIncome: false,
});

// ── component ─────────────────────────────────────────────────────────────

export default function TransactionsPage() {
  const fmt = useCurrencyFormatter();

  const [tab, setTab] = useState<Tab>('transactions');

  const [categories, setCategories] = useState<TransactionCategories>({
    expense: [],
    income: [],
  });

  // ── transactions state ──────────────────────────────────────────────────
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [txLoading, setTxLoading] = useState(true);
  const [txError, setTxError] = useState<string | null>(null);
  const [txSuccess, setTxSuccess] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [filterFrom, setFilterFrom] = useState('');
  const [filterTo, setFilterTo] = useState('');
  const [filterCategory, setFilterCategory] = useState('');
  const [filterKeyword, setFilterKeyword] = useState('');

  const [editingId, setEditingId] = useState<string | null>(null);
  const [txForm, setTxForm] = useState<TxForm>({
    amount: '',
    date: todayStr(),
    category: '',
    description: '',
    isIncome: false,
  });
  const amountRef = useRef<HTMLInputElement>(null);

  // ── budgets state ────────────────────────────────────────────────────────
  const [budgetYear, setBudgetYear] = useState(currentYear);
  const [budgetMonth, setBudgetMonth] = useState(currentMonth);
  const [budgets, setBudgets] = useState<Budget[]>([]);
  const [usage, setUsage] = useState<TransactionBudgetUsage[]>([]);
  const [budgetLoading, setBudgetLoading] = useState(false);
  const [budgetError, setBudgetError] = useState<string | null>(null);
  const [budgetSuccess, setBudgetSuccess] = useState<string | null>(null);
  const [limitInputs, setLimitInputs] = useState<Record<string, string>>({});
  const [savingCategory, setSavingCategory] = useState<string | null>(null);

  // ── load categories ─────────────────────────────────────────────────────
  useEffect(() => {
    apiFetch('/api/transactions/categories', TransactionCategoriesSchema)
      .then((cats) => {
        setCategories(cats);
        setTxForm((f) => ({ ...f, category: f.category || cats.expense[0] || '' }));
      })
      .catch(() => {});
  }, []);

  // ── load transactions ────────────────────────────────────────────────────
  const loadTransactions = async () => {
    setTxLoading(true);
    setTxError(null);
    try {
      const params = new URLSearchParams();
      if (filterFrom) params.set('from', new Date(`${filterFrom}T00:00:00`).toISOString());
      if (filterTo) params.set('to', new Date(`${filterTo}T23:59:59`).toISOString());
      if (filterCategory) params.set('category', filterCategory);
      if (filterKeyword) params.set('keyword', filterKeyword);
      setTransactions(await apiFetch(`/api/transactions?${params}`, TransactionListSchema));
    } catch (e) {
      setTxError(e instanceof Error ? e.message : 'Failed to load transactions');
    } finally {
      setTxLoading(false);
    }
  };

  useEffect(() => {
    loadTransactions();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterFrom, filterTo, filterCategory, filterKeyword]);

  // ── load budgets ─────────────────────────────────────────────────────────
  const loadBudgets = async () => {
    setBudgetLoading(true);
    setBudgetError(null);
    try {
      const [b, u] = await Promise.all([
        apiFetch(`/api/budgets?year=${budgetYear}&month=${budgetMonth}`, BudgetListSchema),
        apiFetch(`/api/budgets/usage?year=${budgetYear}&month=${budgetMonth}`, TransactionBudgetUsageListSchema),
      ]);
      setBudgets(b);
      setUsage(u);
      const inputs: Record<string, string> = {};
      for (const cat of categories.expense) {
        const existing = b.find((x) => x.category === cat);
        inputs[cat] = existing ? String(existing.limitAmount) : '';
      }
      setLimitInputs(inputs);
    } catch (e) {
      setBudgetError(e instanceof Error ? e.message : 'Failed to load budgets');
    } finally {
      setBudgetLoading(false);
    }
  };

  useEffect(() => {
    if (tab === 'budgets' && categories.expense.length > 0) loadBudgets();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, budgetYear, budgetMonth, categories]);

  // ── form helpers ─────────────────────────────────────────────────────────
  const startEdit = (tx: Transaction) => {
    const isIncome = tx.amount > 0;
    setEditingId(tx.id);
    setTxForm({
      amount: String(Math.abs(tx.amount)),
      date: tx.date.slice(0, 10),
      category: tx.category,
      description: tx.description ?? '',
      isIncome,
    });
    setTxError(null);
    setTxSuccess(null);
    amountRef.current?.focus();
  };

  const cancelEdit = () => {
    setEditingId(null);
    setTxForm(emptyTxForm(categories));
    setTxError(null);
    setTxSuccess(null);
  };

  const updateForm = (field: keyof TxForm, value: string | boolean) => {
    setTxForm((f) => {
      const next = { ...f, [field]: value };
      if (field === 'isIncome') {
        const cats = value ? categories.income : categories.expense;
        next.category = cats[0] ?? '';
      }
      return next;
    });
    setTxSuccess(null);
  };

  const handleTxSubmit = async (e: { preventDefault(): void }) => {
    e.preventDefault();
    setTxError(null);
    setTxSuccess(null);

    const amount = Number(txForm.amount);
    if (!Number.isFinite(amount) || amount <= 0) {
      setTxError('Enter a valid amount greater than zero.');
      return;
    }
    if (!txForm.category) {
      setTxError('Select a category.');
      return;
    }

    const signedAmount = txForm.isIncome ? amount : -amount;
    const body = JSON.stringify({
      amount: signedAmount,
      date: new Date(`${txForm.date}T12:00:00`).toISOString(),
      category: txForm.category,
      description: txForm.description.trim() || null,
    });

    setSubmitting(true);
    try {
      if (editingId) {
        await apiFetch(`/api/transactions/${editingId}`, { method: 'PUT', body });
        setTxSuccess('Transaction updated.');
      } else {
        await apiFetch('/api/transactions', TransactionSchema, { method: 'POST', body });
        setTxSuccess('Transaction added.');
      }
      setEditingId(null);
      setTxForm(emptyTxForm(categories));
      await loadTransactions();
    } catch (err) {
      setTxError(err instanceof Error ? err.message : 'Failed to save transaction');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Delete this transaction?')) return;
    try {
      await apiFetch(`/api/transactions/${id}`, { method: 'DELETE' });
      if (editingId === id) cancelEdit();
      await loadTransactions();
    } catch (e) {
      setTxError(e instanceof Error ? e.message : 'Failed to delete');
    }
  };

  const saveBudget = async (category: string) => {
    const raw = limitInputs[category];
    const limit = Number(raw);
    if (!raw || !Number.isFinite(limit) || limit < 0) {
      setBudgetError('Enter a valid limit amount.');
      return;
    }
    setBudgetError(null);
    setBudgetSuccess(null);
    setSavingCategory(category);
    try {
      await apiFetch('/api/budgets', {
        method: 'PUT',
        body: JSON.stringify({ year: budgetYear, month: budgetMonth, category, limitAmount: limit }),
      });
      setBudgetSuccess(`Budget saved for ${category}.`);
      await loadBudgets();
    } catch (e) {
      setBudgetError(e instanceof Error ? e.message : 'Failed to save budget');
    } finally {
      setSavingCategory(null);
    }
  };

  const getUsage = (category: string): TransactionBudgetUsage | undefined =>
    usage.find((u) => u.category === category);

  const totalIncome = transactions.filter((t) => t.amount > 0).reduce((s, t) => s + t.amount, 0);
  const totalExpenses = transactions.filter((t) => t.amount < 0).reduce((s, t) => s + Math.abs(t.amount), 0);
  const allCategories = [...categories.expense, ...categories.income];

  return (
    <div>
      {/* Page header */}
      <div className="page-header">
        <h1 className="page-title">Transactions & Budgets</h1>
        <p className="page-subtitle">Track income, expenses and set monthly spending limits.</p>
      </div>

      {/* Tabs */}
      <ul className="nav nav-tabs mb-4">
        <li className="nav-item">
          <button type="button" className={`nav-link${tab === 'transactions' ? ' active' : ''}`} onClick={() => setTab('transactions')}>
            Transactions
          </button>
        </li>
        <li className="nav-item">
          <button type="button" className={`nav-link${tab === 'budgets' ? ' active' : ''}`} onClick={() => setTab('budgets')}>
            Budgets
          </button>
        </li>
      </ul>

      {/* ── TRANSACTIONS TAB ── */}
      {tab === 'transactions' && (
        <div>
          {txError && <div className="alert alert-danger mb-3">{txError}</div>}
          {txSuccess && <div className="alert alert-success mb-3">{txSuccess}</div>}

          <div className="row g-4">
            {/* Form */}
            <div className="col-lg-4">
              <div className="card">
                <div className="card-body">
                  <h6 className="card-title mb-3">{editingId ? 'Edit Transaction' : 'New Transaction'}</h6>
                  <form onSubmit={handleTxSubmit}>
                    <div className="mb-3">
                      <div className="btn-group w-100">
                        <button
                          type="button"
                          className={`btn btn-sm ${!txForm.isIncome ? 'btn-danger' : 'btn-outline-secondary'}`}
                          onClick={() => updateForm('isIncome', false)}
                          disabled={submitting}
                        >
                          Expense
                        </button>
                        <button
                          type="button"
                          className={`btn btn-sm ${txForm.isIncome ? 'btn-success' : 'btn-outline-secondary'}`}
                          onClick={() => updateForm('isIncome', true)}
                          disabled={submitting}
                        >
                          Income
                        </button>
                      </div>
                    </div>

                    <div className="mb-3">
                      <label className="form-label" htmlFor="tx-amount">Amount</label>
                      <input
                        id="tx-amount"
                        ref={amountRef}
                        className="form-control"
                        type="number"
                        min="0.01"
                        step="0.01"
                        value={txForm.amount}
                        onChange={(e) => updateForm('amount', e.target.value)}
                        placeholder="0.00"
                        disabled={submitting}
                        required
                      />
                    </div>

                    <div className="mb-3">
                      <label className="form-label" htmlFor="tx-date">Date</label>
                      <DatePicker
                        value={txForm.date}
                        onChange={(v) => updateForm('date', v)}
                      />
                    </div>

                    <div className="mb-3">
                      <label className="form-label" htmlFor="tx-category">Category</label>
                      <select
                        id="tx-category"
                        className="form-select"
                        value={txForm.category}
                        onChange={(e) => updateForm('category', e.target.value)}
                        disabled={submitting}
                        required
                      >
                        {(txForm.isIncome ? categories.income : categories.expense).map((c) => (
                          <option key={c} value={c}>{c}</option>
                        ))}
                      </select>
                    </div>

                    <div className="mb-3">
                      <label className="form-label" htmlFor="tx-desc">Description</label>
                      <input
                        id="tx-desc"
                        className="form-control"
                        type="text"
                        value={txForm.description}
                        onChange={(e) => updateForm('description', e.target.value)}
                        placeholder="Optional"
                        disabled={submitting}
                      />
                    </div>

                    <div className="d-flex gap-2">
                      <button
                        type="submit"
                        className={`btn flex-grow-1 ${txForm.isIncome ? 'btn-success' : 'btn-primary'}`}
                        disabled={submitting}
                      >
                        {submitting ? 'Saving…' : editingId ? 'Update' : 'Add'}
                      </button>
                      {editingId && (
                        <button
                          type="button"
                          className="btn btn-outline-secondary"
                          onClick={cancelEdit}
                          disabled={submitting}
                        >
                          Cancel
                        </button>
                      )}
                    </div>
                  </form>
                </div>
              </div>
            </div>

            {/* List */}
            <div className="col-lg-8">
              {/* Summary metric cards */}
              {!txLoading && transactions.length > 0 && (
                <div className="row g-3 mb-4">
                  <div className="col-6">
                    <div className="metric-card c-success">
                      <div className="metric-label">Income</div>
                      <div className="metric-value up">{fmt(totalIncome)}</div>
                      <div className="metric-sub">{transactions.filter((t) => t.amount > 0).length} transactions</div>
                    </div>
                  </div>
                  <div className="col-6">
                    <div className="metric-card c-danger">
                      <div className="metric-label">Expenses</div>
                      <div className="metric-value down">{fmt(totalExpenses)}</div>
                      <div className="metric-sub">{transactions.filter((t) => t.amount < 0).length} transactions</div>
                    </div>
                  </div>
                </div>
              )}

              {/* Filters */}
              <div className="card mb-3">
                <div className="card-body py-2">
                  <div className="row g-2 align-items-end">
                    <div className="col-sm-3">
                      <label className="form-label" htmlFor="filter-from">From</label>
                      <DatePicker value={filterFrom} onChange={setFilterFrom} placeholder="From" />
                    </div>
                    <div className="col-sm-3">
                      <label className="form-label" htmlFor="filter-to">To</label>
                      <DatePicker value={filterTo} onChange={setFilterTo} placeholder="To" min={filterFrom} />
                    </div>
                    <div className="col-sm-3">
                      <label className="form-label" htmlFor="filter-cat">Category</label>
                      <select id="filter-cat" className="form-select form-select-sm" value={filterCategory} onChange={(e) => setFilterCategory(e.target.value)}>
                        <option value="">All</option>
                        {allCategories.map((c) => <option key={c} value={c}>{c}</option>)}
                      </select>
                    </div>
                    <div className="col-sm-3">
                      <label className="form-label" htmlFor="filter-kw">Search</label>
                      <input id="filter-kw" className="form-control form-control-sm" type="text" value={filterKeyword} onChange={(e) => setFilterKeyword(e.target.value)} placeholder="Keyword…" />
                    </div>
                  </div>
                </div>
              </div>

              {/* Transaction list */}
              <div className="card">
                <div className="card-body p-0">
                  {txLoading ? (
                    <div className="loading-center">
                      <div className="spinner-border spinner-border-sm text-primary" role="status">
                        <span className="visually-hidden">Loading…</span>
                      </div>
                    </div>
                  ) : transactions.length === 0 ? (
                    <p className="text-muted small text-center py-5 mb-0">No transactions found.</p>
                  ) : (
                    <div className="list-group list-group-flush" style={{ borderRadius: 'inherit' }}>
                      {[...transactions]
                        .sort((a, b) => b.date.localeCompare(a.date))
                        .map((tx) => {
                          const isIncome = tx.amount > 0;
                          const isEditing = editingId === tx.id;
                          return (
                            <div
                              key={tx.id}
                              className="list-group-item d-flex justify-content-between align-items-center gap-2"
                              style={{ background: isEditing ? 'var(--color-primary-light)' : undefined }}
                            >
                              <div className="flex-grow-1 min-w-0">
                                <div className="d-flex align-items-center gap-2">
                                  <span className="tx-category-badge">{tx.category}</span>
                                  <span className="text-muted" style={{ fontSize: '0.8rem' }}>
                                    {new Date(tx.date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                                  </span>
                                </div>
                                {tx.description && (
                                  <div className="text-muted text-truncate mt-1" style={{ fontSize: '0.8rem' }}>{tx.description}</div>
                                )}
                              </div>
                              <div className="d-flex align-items-center gap-2 flex-shrink-0">
                                <span className={`fw-semibold ${isIncome ? 'text-success' : 'text-danger'}`} style={{ fontSize: '0.9rem' }}>
                                  {isIncome ? '+' : '-'}{fmt(Math.abs(tx.amount))}
                                </span>
                                <button type="button" className="pf-btn-icon edit" onClick={() => startEdit(tx)} title="Edit">✎</button>
                                <button type="button" className="pf-btn-icon danger" onClick={() => handleDelete(tx.id)} title="Delete">✕</button>
                              </div>
                            </div>
                          );
                        })}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── BUDGETS TAB ── */}
      {tab === 'budgets' && (
        <div>
          {budgetError && <div className="alert alert-danger mb-3">{budgetError}</div>}
          {budgetSuccess && <div className="alert alert-success mb-3">{budgetSuccess}</div>}

          {/* Month selector */}
          <div className="d-flex align-items-center gap-3 mb-4">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              onClick={() => { if (budgetMonth === 1) { setBudgetMonth(12); setBudgetYear((y) => y - 1); } else { setBudgetMonth((m) => m - 1); } }}
            >‹</button>
            <span className="fw-semibold">{monthLabel(budgetYear, budgetMonth)}</span>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              onClick={() => { if (budgetMonth === 12) { setBudgetMonth(1); setBudgetYear((y) => y + 1); } else { setBudgetMonth((m) => m + 1); } }}
            >›</button>
          </div>

          {budgetLoading ? (
            <div className="loading-center">
              <div className="spinner-border spinner-border-sm text-primary" role="status">
                <span className="visually-hidden">Loading…</span>
              </div>
            </div>
          ) : (
            <div className="row g-3">
              {categories.expense.map((cat) => {
                const u = getUsage(cat);
                const pct = u ? Math.min(u.usagePercent, 100) : 0;
                const over = u && u.usagePercent > 100;
                const hasLimit = !!budgets.find((b) => b.category === cat);

                return (
                  <div className="col-md-6" key={cat}>
                    <div className="card h-100">
                      <div className="card-body">
                        <div className="d-flex justify-content-between align-items-center mb-2">
                          <span className="fw-semibold" style={{ fontSize: '0.9rem' }}>{cat}</span>
                          {u && (
                            <span className={`small ${over ? 'text-danger fw-semibold' : 'text-muted'}`}>
                              {fmt(u.spent)} / {hasLimit ? fmt(u.limit) : '—'}
                            </span>
                          )}
                        </div>

                        {hasLimit && u && (
                          <div className="mb-2">
                            <div className="progress" style={{ height: 6 }}>
                              <div
                                className={`progress-bar ${over ? 'bg-danger' : pct > 80 ? 'bg-warning' : 'bg-primary'}`}
                                style={{ width: `${pct}%` }}
                              />
                            </div>
                            <div className="d-flex justify-content-between mt-1">
                              <span className="text-muted" style={{ fontSize: '0.72rem' }}>{u.usagePercent}% used</span>
                              <span className={over ? 'text-danger' : 'text-muted'} style={{ fontSize: '0.72rem' }}>
                                {over ? `${fmt(Math.abs(u.remaining))} over` : `${fmt(u.remaining)} left`}
                              </span>
                            </div>
                          </div>
                        )}

                        {!hasLimit && u && u.spent > 0 && (
                          <p className="small text-muted mb-2">{fmt(u.spent)} spent — no limit set</p>
                        )}

                        <div className="input-group input-group-sm mt-2">
                          <span className="input-group-text">Limit</span>
                          <input
                            className="form-control"
                            type="number"
                            min="0"
                            step="1"
                            placeholder="e.g. 500"
                            value={limitInputs[cat] ?? ''}
                            onChange={(e) => setLimitInputs((prev) => ({ ...prev, [cat]: e.target.value }))}
                          />
                          <button
                            type="button"
                            className="btn btn-primary"
                            onClick={() => saveBudget(cat)}
                            disabled={savingCategory === cat}
                          >
                            {savingCategory === cat ? '…' : 'Save'}
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
