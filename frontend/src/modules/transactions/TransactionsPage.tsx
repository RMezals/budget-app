import { apiFetch } from '@/api/client';
import { useCurrencyFormatter } from '@/hooks/useCurrencyFormatter';
import { useEffect, useState } from 'react';

interface Transaction {
  id: string;
  amount: number;
  category: string;
  description?: string;
  date: string;
}

interface CategoriesData {
  expense: string[];
  income: string[];
}

const getLocalDateString = (dateInput?: string | Date) => {
  const d = dateInput ? new Date(dateInput) : new Date();
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

export default function TransactionsPage() {
  const fmt = useCurrencyFormatter();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [categories, setCategories] = useState<CategoriesData>({ expense: [], income: [] });
  const [loading, setLoading] = useState(true);
  
  const [amount, setAmount] = useState<string>('');
  const [category, setCategory] = useState<string>('');
  const [description, setDescription] = useState<string>('');
  const [date, setDate] = useState<string>(getLocalDateString());
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);

  const [filterKeyword, setFilterKeyword] = useState<string>('');
  const [filterCategory, setFilterCategory] = useState<string>('');
  const [filterStartDate, setFilterStartDate] = useState<string>('');
  const [filterEndDate, setFilterEndDate] = useState<string>('');
  const [filterMinAmount, setFilterMinAmount] = useState<string>('');
  const [filterMaxAmount, setFilterMaxAmount] = useState<string>('');

  useEffect(() => {
    let cancelled = false;

    const loadData = async () => {
      try {
        const [transactionsData, categoriesData] = await Promise.all([
          apiFetch<Transaction[]>('/api/transactions'),
          apiFetch<CategoriesData>('/api/transactions/categories'),
        ]);

        if (cancelled) return;

        setTransactions(transactionsData);
        setCategories(categoriesData);

        if (categoriesData.expense.length > 0) {
          setCategory(categoriesData.expense[0]);
        }
      } catch (err) {
        if (!cancelled) {
          console.error(err);
          setError('Unable to load transactions data.');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    loadData();
    return () => {
      cancelled = true;
    };
  }, []);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);

    const selectedDateISO = new Date(date).toISOString();

    const requestBody = {
      amount: Number.parseFloat(amount),
      date: selectedDateISO,
      category: category,
      description: description.trim() || undefined,
    };

    try {
      if (editingId) {
        await apiFetch(`/api/transactions/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(requestBody),
        });
      } else {
        await apiFetch('/api/transactions', {
          method: 'POST',
          body: JSON.stringify(requestBody),
        });
      }

      const updatedTransactions = await apiFetch<Transaction[]>('/api/transactions');
      setTransactions(updatedTransactions);

      resetForm();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save transaction');
    } finally {
      setSubmitting(false);
    }
  };

  const startEdit = (tx: Transaction) => {
    setError(null);
    setEditingId(tx.id);
    setAmount(String(tx.amount));
    setCategory(tx.category);
    setDescription(tx.description || '');
    setDate(getLocalDateString(tx.date));
  };

  const resetForm = () => {
    setEditingId(null);
    setAmount('');
    setDescription('');
    setDate(getLocalDateString());
    if (categories.expense.length > 0) {
      setCategory(categories.expense[0]);
    }
  };

  const clearFilters = () => {
    setFilterKeyword('');
    setFilterCategory('');
    setFilterStartDate('');
    setFilterEndDate('');
    setFilterMinAmount('');
    setFilterMaxAmount('');
  };

  const handleDelete = async (id: string, currentAmount: number) => {
    const confirmed = window.confirm(`Delete transaction for ${fmt(currentAmount)}?`);
    if (!confirmed) return;

    setError(null);
    try {
      await apiFetch(`/api/transactions/${id}`, { method: 'DELETE' });
      setTransactions((prev) => prev.filter((t) => t.id !== id));
      
      if (editingId === id) {
        resetForm();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete transaction');
    }
  };

  const formatDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(new Date(dateStr));
  };

  const filteredTransactions = transactions.filter((tx) => {
    if (filterKeyword.trim()) {
      const kw = filterKeyword.toLowerCase();
      const descMatch = tx.description?.toLowerCase().includes(kw) ?? false;
      const catMatch = tx.category.toLowerCase().includes(kw);
      if (!descMatch && !catMatch) return false;
    }

    if (filterCategory && tx.category !== filterCategory) {
      return false;
    }

    const txDateStr = getLocalDateString(tx.date);
    if (filterStartDate && txDateStr < filterStartDate) {
      return false;
    }
    if (filterEndDate && txDateStr > filterEndDate) {
      return false;
    }

    if (filterMinAmount !== '' && tx.amount < Number.parseFloat(filterMinAmount)) {
      return false;
    }
    if (filterMaxAmount !== '' && tx.amount > Number.parseFloat(filterMaxAmount)) {
      return false;
    }

    return true;
  });

  if (loading) {
    return (
      <div className="d-flex justify-content-center py-5">
        <output className="spinner-border text-primary">
          <span className="visually-hidden">Loading...</span>
        </output>
      </div>
    );
  }

  return (
    <div className="text-start">
      <h4 className="mb-4">Transactions</h4>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      <div className="row g-4">
        <div className="col-lg-4">
          <div className="card border-0 shadow-sm">
            <div className="card-body">
              <h6 className="card-title mb-3 text-primary">
                {editingId ? 'Edit Transaction' : 'Add New Transaction'}
              </h6>
              
              <form onSubmit={handleSubmit}>
                <div className="mb-3">
                  <label htmlFor="date" className="form-label small fw-semibold">
                    Date
                  </label>
                  <input
                    id="date"
                    type="date"
                    className="form-control form-control-sm"
                    value={date}
                    onChange={(e) => setDate(e.target.value)}
                    required
                  />
                </div>

                <div className="mb-3">
                  <label htmlFor="amount" className="form-label small fw-semibold">
                    Amount
                  </label>
                  <div className="input-group input-group-sm">
                    <input
                      id="amount"
                      type="number"
                      step="0.01"
                      className="form-control"
                      placeholder="e.g. -15.50 or 500"
                      value={amount}
                      onChange={(e) => setAmount(e.target.value)}
                      required
                    />
                    
                  </div>
                  <div className="form-text small text-muted">
                    Negative for expenses, positive for income.
                  </div>
                </div>

                <div className="mb-3">
                  <label htmlFor="category" className="form-label small fw-semibold">
                    Category
                  </label>
                  <select
                    id="category"
                    className="form-select form-select-sm"
                    value={category}
                    onChange={(e) => setCategory(e.target.value)}
                    required
                  >
                    <optgroup label="Expenses">
                      {categories.expense.map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </optgroup>
                    <optgroup label="Income">
                      {categories.income.map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </optgroup>
                  </select>
                </div>

                <div className="mb-3">
                  <label htmlFor="description" className="form-label small fw-semibold">
                    Description
                  </label>
                  <input
                    id="description"
                    type="text"
                    className="form-control form-control-sm"
                    placeholder="e.g. Grocery shopping"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                  />
                </div>

                <div className="d-flex gap-2">
                  <button
                    type="submit"
                    className={`btn btn-sm w-100 ${editingId ? 'btn-success' : 'btn-primary'}`}
                    disabled={submitting}
                  >
                    {submitting ? 'Saving...' : editingId ? 'Update Changes' : 'Save Transaction'}
                  </button>
                  
                  {editingId && (
                    <button
                      type="button"
                      className="btn btn-outline-secondary btn-sm"
                      onClick={resetForm}
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

        <div className="col-lg-8">
          <div className="card border-0 shadow-sm mb-4">
            <div className="card-body bg-light rounded">
              <h6 className="card-title mb-3 text-secondary small fw-bold text-uppercase">
                Filter & Search
              </h6>
              <div className="row g-2">
                <div className="col-md-4">
                  <input
                    type="text"
                    className="form-control form-control-sm"
                    placeholder="Search keyword..."
                    value={filterKeyword}
                    onChange={(e) => setFilterKeyword(e.target.value)}
                  />
                </div>
                
                <div className="col-md-4">
                  <select
                    className="form-select form-select-sm"
                    value={filterCategory}
                    onChange={(e) => setFilterCategory(e.target.value)}
                  >
                    <option value="">All Categories</option>
                    <optgroup label="Expenses">
                      {categories.expense.map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </optgroup>
                    <optgroup label="Income">
                      {categories.income.map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </optgroup>
                  </select>
                </div>

                <div className="col-md-4 text-md-end">
                  {(filterKeyword ||
                    filterCategory ||
                    filterStartDate ||
                    filterEndDate ||
                    filterMinAmount ||
                    filterMaxAmount) && (
                    <button 
                      type="button" 
                      className="btn btn-link btn-sm text-decoration-none p-0 pt-1"
                      onClick={clearFilters}
                    >
                      Clear Filters
                    </button>
                  )}
                </div>

                <div className="col-6 col-md-3">
                  <label htmlFor="filterStartDate" className="form-label mb-0 small text-muted">
                    From Date
                  </label>
                  <input
                    id="filterStartDate"
                    type="date"
                    className="form-control form-control-sm"
                    value={filterStartDate}
                    onChange={(e) => setFilterStartDate(e.target.value)}
                  />
                </div>

                <div className="col-6 col-md-3">
                  <label htmlFor="filterEndDate" className="form-label mb-0 small text-muted">
                    To Date
                  </label>
                  <input
                    id="filterEndDate"
                    type="date"
                    className="form-control form-control-sm"
                    value={filterEndDate}
                    onChange={(e) => setFilterEndDate(e.target.value)}
                  />
                </div>

                <div className="col-6 col-md-3">
                  <label htmlFor="filterMinAmount" className="form-label mb-0 small text-muted">
                    Min Amount
                  </label>
                  <input
                    id="filterMinAmount"
                    type="number"
                    step="0.01"
                    className="form-control form-control-sm"
                    placeholder="Min €"
                    value={filterMinAmount}
                    onChange={(e) => setFilterMinAmount(e.target.value)}
                  />
                </div>

                <div className="col-6 col-md-3">
                  <label htmlFor="filterMaxAmount" className="form-label mb-0 small text-muted">
                    Max Amount
                  </label>
                  <input
                    id="filterMaxAmount"
                    type="number"
                    step="0.01"
                    className="form-control form-control-sm"
                    placeholder="Max €"
                    value={filterMaxAmount}
                    onChange={(e) => setFilterMaxAmount(e.target.value)}
                  />
                </div>
              </div>
            </div>
          </div>

          <div className="card border-0 shadow-sm">
            <div className="card-body">
              <div className="d-flex justify-content-between align-items-center mb-3">
                <h6 className="card-title mb-0">History</h6>
                <span className="text-muted small">
                  Showing {filteredTransactions.length} of {transactions.length} total
                </span>
              </div>

              {filteredTransactions.length === 0 ? (
                <p className="text-muted small mb-0 py-3 text-center">
                  No transactions match the selected filters.
                </p>
              ) : (
                <div className="table-responsive">
                  <table className="table align-middle mb-0 small">
                    <thead>
                      <tr>
                        <th scope="col">Date</th>
                        <th scope="col">Category</th>
                        <th scope="col">Description</th>
                        <th scope="col" className="text-end">
                          Amount
                        </th>
                        <th scope="col" className="text-end">
                          Actions
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredTransactions.map((tx) => (
                        <tr key={tx.id} className={editingId === tx.id ? 'table-light' : undefined}>
                          <td className="text-nowrap">{formatDate(tx.date)}</td>
                          <td>
                            <span className="badge text-bg-light border text-secondary">
                              {tx.category}
                            </span>
                          </td>
                          <td className="text-muted">{tx.description || '—'}</td>
                          <td
                            className={`text-end fw-semibold text-nowrap ${
                              tx.amount < 0 ? 'text-danger' : 'text-success'
                            }`}
                          >
                            {tx.amount < 0 ? '' : '+'}
                            {fmt(tx.amount)}
                          </td>
                          <td className="text-end">
                            <div className="d-flex justify-content-end gap-2">
                              <button
                                type="button"
                                className="btn btn-link btn-sm p-0 text-primary text-decoration-none"
                                onClick={() => startEdit(tx)}
                              >
                                Edit
                              </button>
                              <span className="text-muted">|</span>
                              <button
                                type="button"
                                className="btn btn-link btn-sm p-0 text-danger text-decoration-none"
                                onClick={() => handleDelete(tx.id, tx.amount)}
                              >
                                Delete
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
      </div>
    </div>
  );
}
