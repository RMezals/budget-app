import { apiFetch } from '@/api/client';
import {
  MonthlyPerformanceListSchema,
  NetWorthHistoryPointListSchema,
  PortfolioGainLossSchema,
} from '@/api/schemas';
import type {
  AddAmountRequest,
  AddPriceRequest,
  CreateAssetRequest,
  CreateLiabilityRequest,
  MonthlyPerformance,
  NetWorthHistoryPoint,
  PortfolioGainLoss,
  UpdateAssetRequest,
  UpdateLiabilityRequest,
} from '@/api/types';
import DatePicker from '@/components/DatePicker';
import { useCurrencyFormatter } from '@/hooks/useCurrencyFormatter';
import MonthPicker from '@/modules/portfolio/MonthPicker';
import { usePortfolio } from '@/modules/portfolio/hooks/usePortfolio';
import './PortfolioPage.css';
import { useEffect, useState } from 'react';
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

// ── Constants ──────────────────────────────────────────────────────────────

// Colour mapping used for the allocation bar chart — each asset type gets a distinct colour
const ALLOC_COLORS: Record<string, string> = {
  Stock: '#2563eb',
  ETF: '#0891b2',
  Crypto: '#d97706',
  Bond: '#16a34a',
  RealEstate: '#dc2626',
  Cash: '#64748b',
  Commodity: '#ca8a04',
  Other: '#6b7280',
};

// Returns the chart colour for a given asset type, falling back to blue for unknown types
function allocColor(type: string) {
  return ALLOC_COLORS[type] ?? '#2563eb';
}

// Background + text colour pairs used for the coloured type badge pills on each row
const TYPE_STYLES: Record<string, { bg: string; color: string }> = {
  Stock: { bg: '#eff6ff', color: '#1d4ed8' },
  ETF: { bg: '#ecfeff', color: '#0e7490' },
  Crypto: { bg: '#fffbeb', color: '#b45309' },
  Bond: { bg: '#f0fdf4', color: '#15803d' },
  RealEstate: { bg: '#fef2f2', color: '#b91c1c' },
  Cash: { bg: '#f8fafc', color: '#475569' },
  Commodity: { bg: '#fefce8', color: '#a16207' },
  Other: { bg: '#f9fafb', color: '#6b7280' },
  Mortgage: { bg: '#fef2f2', color: '#b91c1c' },
  CarLoan: { bg: '#fffbeb', color: '#b45309' },
  StudentLoan: { bg: '#eff6ff', color: '#1d4ed8' },
  CreditCard: { bg: '#fff7ed', color: '#c2410c' },
  PersonalLoan: { bg: '#f0f9ff', color: '#0369a1' },
};

// ── Helpers ────────────────────────────────────────────────────────────────

// Returns today's date as YYYY-MM-DD, used as the default for date inputs and as the max date
function today() {
  return new Date().toISOString().split('T')[0];
}

// Displays a coloured gain/loss badge with an up/down arrow and percentage
function GainBadge({ value, percent }: { value: number; percent: number }) {
  const pos = value >= 0;
  return (
    <span className={`pf-gain ${pos ? 'positive' : 'negative'}`}>
      {pos ? '▲' : '▼'} {pos ? '+' : ''}
      {value.toFixed(2)} ({pos ? '+' : ''}
      {percent.toFixed(2)}%)
    </span>
  );
}

// Renders a small coloured pill showing an asset or liability type (e.g. "Stock", "Mortgage")
function TypeBadge({ type }: { type: string }) {
  const s = TYPE_STYLES[type] ?? { bg: '#f3f4f6', color: '#374151' };
  return (
    <span className="pf-type-badge" style={{ background: s.bg, color: s.color }}>
      {type}
    </span>
  );
}

// ── Main page ──────────────────────────────────────────────────────────────

// Main portfolio page — displays assets, liabilities, performance chart, and net worth history
export default function PortfolioPage() {
  const fmt = useCurrencyFormatter();
  const {
    netWorth,
    assets,
    allocation,
    liabilities,
    assetTypes,
    liabilityTypes,
    loading,
    error,
    reload, // call after mutations to refresh all portfolio data
  } = usePortfolio();

  // Controls which tab panel is displayed; defaults to the assets list
  const [tab, setTab] = useState<'assets' | 'liabilities' | 'performance' | 'networth'>('assets');

  // ── Performance state ──
  // globalGainLoss is the unrealised gain/loss across all current assets
  const [globalGainLoss, setGlobalGainLoss] = useState<PortfolioGainLoss | null>(null);
  const [monthlyPerf, setMonthlyPerf] = useState<MonthlyPerformance[]>([]);
  const [perfLoading, setPerfLoading] = useState(false);
  // perfLoaded prevents re-fetching on every tab switch; only loads once unless the user clicks Reload
  const [perfLoaded, setPerfLoaded] = useState(false);
  const now = new Date();
  // Default date range for performance: one year ago to this month
  const [perfFrom, setPerfFrom] = useState(
    `${now.getFullYear() - 1}-${String(now.getMonth() + 1).padStart(2, '0')}`,
  );
  const [perfTo, setPerfTo] = useState(
    `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`,
  );

  // Re-fetches unrealised gain/loss whenever the assets list changes (e.g. after a price update)
  // biome-ignore lint/correctness/useExhaustiveDependencies: re-fetch gain-loss whenever assets list changes
  useEffect(() => {
    apiFetch('/api/assets/gain-loss', PortfolioGainLossSchema)
      .then(setGlobalGainLoss)
      .catch(() => {}); // Silently ignore errors; the metric card will show "—"
  }, [assets]);

  // Fetches monthly performance data for the selected date range
  async function loadPerformance() {
    setPerfLoading(true);
    try {
      // Convert YYYY-MM strings to full ISO timestamps (first day of each month)
      const from = new Date(`${perfFrom}-01`).toISOString();
      const to = new Date(`${perfTo}-01`).toISOString();
      const data = await apiFetch(
        `/api/assets/performance?from=${from}&to=${to}`,
        MonthlyPerformanceListSchema,
      );
      setMonthlyPerf(data);
      setPerfLoaded(true);
    } finally {
      setPerfLoading(false);
    }
  }

  // Handles tab switching and lazy-loads data for tabs that need it (performance / net worth)
  function switchTab(t: 'assets' | 'liabilities' | 'performance' | 'networth') {
    setTab(t);
    // Only fetch on first visit to each tab; the user can manually reload if needed
    if (t === 'performance' && !perfLoaded) loadPerformance();
    if (t === 'networth' && !nwLoaded) loadNwHistory();
  }

  // ── Net worth history state ──
  const [nwHistory, setNwHistory] = useState<NetWorthHistoryPoint[]>([]);
  const [nwLoading, setNwLoading] = useState(false);
  // nwLoaded prevents re-fetching on tab re-visit
  const [nwLoaded, setNwLoaded] = useState(false);
  // Default date range: last 6 months
  const sixMonthsAgo = new Date();
  sixMonthsAgo.setMonth(sixMonthsAgo.getMonth() - 6);
  const [nwFrom, setNwFrom] = useState(sixMonthsAgo.toISOString().slice(0, 10));
  const [nwTo, setNwTo] = useState(new Date().toISOString().slice(0, 10));

  // Fetches net worth snapshots for each day in the selected range to power the area chart
  async function loadNwHistory() {
    setNwLoading(true);
    try {
      const data = await apiFetch(
        `/api/networth/history?from=${new Date(nwFrom).toISOString()}&to=${new Date(nwTo).toISOString()}`,
        NetWorthHistoryPointListSchema,
      );
      setNwHistory(data);
      setNwLoaded(true);
    } finally {
      setNwLoading(false);
    }
  }

  // ── Delete confirmation ──
  // deleteTarget identifies what the user wants to delete so the modal can show its name
  const [deleteTarget, setDeleteTarget] = useState<{
    type: 'asset' | 'liability';
    id: string;
    name: string;
  } | null>(null);
  const [deleteError, setDeleteError] = useState('');

  // Executes the delete request for the currently targeted asset or liability
  async function confirmDelete() {
    if (!deleteTarget) return;
    try {
      if (deleteTarget.type === 'asset') {
        await apiFetch(`/api/assets/${deleteTarget.id}`, { method: 'DELETE' });
      } else {
        await apiFetch(`/api/liabilities/${deleteTarget.id}`, { method: 'DELETE' });
      }
      setDeleteTarget(null);
      reload(); // Refresh portfolio data so the deleted item disappears from all lists
    } catch (e) {
      setDeleteError(String(e));
    }
  }

  // ── Asset modal ──
  // assetModal tracks which mode the modal is in: 'create', 'edit', 'price', or null (closed)
  const [assetModal, setAssetModal] = useState<'create' | 'edit' | 'price' | null>(null);
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(null);
  const [assetForm, setAssetForm] = useState({
    name: '',
    type: '',
    quantity: '',
    purchasePrice: '',
    purchaseDate: today(),
  });
  // priceForm is used only in 'price' mode to add a new price data point
  const [priceForm, setPriceForm] = useState({ value: '', date: today() });
  const [assetSaving, setAssetSaving] = useState(false);
  const [assetError, setAssetError] = useState('');
  // assetValidation maps field names to their error messages for inline feedback
  const [assetValidation, setAssetValidation] = useState<Record<string, string>>({});

  // ── Liability modal ──
  const [liabilityModal, setLiabilityModal] = useState<'create' | 'edit' | 'amount' | null>(null);
  const [selectedLiabilityId, setSelectedLiabilityId] = useState<string | null>(null);
  const [liabilityForm, setLiabilityForm] = useState({
    name: '',
    type: '',
    initialAmount: '',
    date: today(),
  });
  // amountForm records a new balance snapshot for an existing liability
  const [amountForm, setAmountForm] = useState({ value: '', date: today() });
  const [liabilitySaving, setLiabilitySaving] = useState(false);
  const [liabilityError, setLiabilityError] = useState('');
  const [liabilityValidation, setLiabilityValidation] = useState<Record<string, string>>({});

  // ── Asset actions ──

  // Resets the asset form and opens the create modal with the first available type pre-selected
  function openCreateAsset() {
    setAssetForm({
      name: '',
      type: assetTypes[0] ?? '',
      quantity: '',
      purchasePrice: '',
      purchaseDate: today(),
    });
    setAssetError('');
    setAssetValidation({});
    setAssetModal('create');
  }

  // Populates the asset form with the existing asset's data and opens edit mode
  function openEditAsset(id: string) {
    const a = assets.find((x) => x.id === id);
    if (!a) return;
    setSelectedAssetId(id);
    setAssetForm({
      name: a.name ?? '',
      type: a.type ?? '',
      quantity: String(a.quantity ?? 0),
      purchasePrice: String(a.purchasePrice ?? 0),
      purchaseDate: today(), // purchaseDate is not editable after creation
    });
    setAssetError('');
    setAssetValidation({});
    setAssetModal('edit');
  }

  // Opens the price-update modal for a specific asset
  function openAddPrice(id: string) {
    setSelectedAssetId(id);
    setPriceForm({ value: '', date: today() });
    setAssetError('');
    setAssetValidation({});
    setAssetModal('price');
  }

  // Validates the asset create/edit form and returns true if all fields are valid
  function validateAsset(): boolean {
    const e: Record<string, string> = {};
    if (!assetForm.name.trim()) e.name = 'Name is required.';
    if (!assetForm.quantity || Number(assetForm.quantity) <= 0)
      e.quantity = 'Enter a positive quantity.';
    // Purchase price and date are only required when creating, not when editing
    if (assetModal === 'create') {
      if (!assetForm.purchasePrice || Number(assetForm.purchasePrice) <= 0)
        e.purchasePrice = 'Enter a purchase price.';
      if (!assetForm.purchaseDate) e.purchaseDate = 'Select a date.';
    }
    setAssetValidation(e);
    return Object.keys(e).length === 0;
  }

  // Validates the price-update form
  function validatePrice(): boolean {
    const e: Record<string, string> = {};
    if (!priceForm.value || Number(priceForm.value) <= 0) e.value = 'Enter a positive price.';
    if (!priceForm.date) e.date = 'Select a date.';
    setAssetValidation(e);
    return Object.keys(e).length === 0;
  }

  // Dispatches the appropriate API call based on which asset modal mode is active
  async function submitAsset() {
    const valid = assetModal === 'price' ? validatePrice() : validateAsset();
    if (!valid) return;
    setAssetSaving(true);
    setAssetError('');
    try {
      if (assetModal === 'create') {
        await apiFetch('/api/assets', {
          method: 'POST',
          body: JSON.stringify({
            name: assetForm.name.trim(),
            type: assetForm.type,
            quantity: Number(assetForm.quantity),
            purchasePrice: Number(assetForm.purchasePrice),
            purchaseDate: assetForm.purchaseDate,
          } satisfies CreateAssetRequest),
        });
      } else if (assetModal === 'edit' && selectedAssetId) {
        // Editing only allows updating name, type, and quantity — not purchase details
        await apiFetch(`/api/assets/${selectedAssetId}`, {
          method: 'PUT',
          body: JSON.stringify({
            name: assetForm.name.trim(),
            type: assetForm.type,
            quantity: Number(assetForm.quantity),
          } satisfies UpdateAssetRequest),
        });
      } else if (assetModal === 'price' && selectedAssetId) {
        // Adding a price creates a historical price record; the backend uses the latest one as current
        await apiFetch(`/api/assets/${selectedAssetId}/prices`, {
          method: 'POST',
          body: JSON.stringify({
            value: Number(priceForm.value),
            date: priceForm.date,
          } satisfies AddPriceRequest),
        });
      }
      setAssetModal(null);
      reload(); // Refresh all data so the new values appear immediately
    } catch (e) {
      setAssetError(String(e));
    } finally {
      setAssetSaving(false);
    }
  }

  // ── Liability actions ──

  // Opens the create liability modal with defaults pre-filled
  function openCreateLiability() {
    setLiabilityForm({ name: '', type: liabilityTypes[0] ?? '', initialAmount: '', date: today() });
    setLiabilityError('');
    setLiabilityValidation({});
    setLiabilityModal('create');
  }

  // Populates the liability form with existing data and opens edit mode
  function openEditLiability(id: string) {
    const l = liabilities.find((x) => x.id === id);
    if (!l) return;
    setSelectedLiabilityId(id);
    // initialAmount is not shown when editing — only name and type are editable
    setLiabilityForm({ name: l.name ?? '', type: l.type ?? '', initialAmount: '', date: today() });
    setLiabilityError('');
    setLiabilityValidation({});
    setLiabilityModal('edit');
  }

  // Opens the balance-update modal for a specific liability
  function openAddAmount(id: string) {
    setSelectedLiabilityId(id);
    setAmountForm({ value: '', date: today() });
    setLiabilityError('');
    setLiabilityValidation({});
    setLiabilityModal('amount');
  }

  // Validates the liability create/edit form
  function validateLiability(): boolean {
    const e: Record<string, string> = {};
    if (!liabilityForm.name.trim()) e.name = 'Name is required.';
    // Initial amount and date are only needed when creating a new liability
    if (liabilityModal === 'create') {
      if (!liabilityForm.initialAmount || Number(liabilityForm.initialAmount) < 0)
        e.initialAmount = 'Enter a valid amount.';
      if (!liabilityForm.date) e.date = 'Select a date.';
    }
    setLiabilityValidation(e);
    return Object.keys(e).length === 0;
  }

  // Validates the balance-update form
  function validateAmount(): boolean {
    const e: Record<string, string> = {};
    if (!amountForm.value || Number(amountForm.value) < 0) e.value = 'Enter a valid amount.';
    if (!amountForm.date) e.date = 'Select a date.';
    setLiabilityValidation(e);
    return Object.keys(e).length === 0;
  }

  // Dispatches the appropriate API call based on the active liability modal mode
  async function submitLiability() {
    const valid = liabilityModal === 'amount' ? validateAmount() : validateLiability();
    if (!valid) return;
    setLiabilitySaving(true);
    setLiabilityError('');
    try {
      if (liabilityModal === 'create') {
        await apiFetch('/api/liabilities', {
          method: 'POST',
          body: JSON.stringify({
            name: liabilityForm.name.trim(),
            type: liabilityForm.type,
            initialAmount: Number(liabilityForm.initialAmount),
            date: liabilityForm.date,
          } satisfies CreateLiabilityRequest),
        });
      } else if (liabilityModal === 'edit' && selectedLiabilityId) {
        await apiFetch(`/api/liabilities/${selectedLiabilityId}`, {
          method: 'PUT',
          body: JSON.stringify({
            name: liabilityForm.name.trim(),
            type: liabilityForm.type,
          } satisfies UpdateLiabilityRequest),
        });
      } else if (liabilityModal === 'amount' && selectedLiabilityId) {
        // Recording a new balance snapshot; the backend uses the most-recent one as "current"
        await apiFetch(`/api/liabilities/${selectedLiabilityId}/amounts`, {
          method: 'POST',
          body: JSON.stringify({
            value: Number(amountForm.value),
            date: amountForm.date,
          } satisfies AddAmountRequest),
        });
      }
      setLiabilityModal(null);
      reload();
    } catch (e) {
      setLiabilityError(String(e));
    } finally {
      setLiabilitySaving(false);
    }
  }

  // Returns the most recent balance snapshot for a liability that is not in the future
  function currentLiabilityBalance(id: string): number {
    const l = liabilities.find((x) => x.id === id);
    if (!l || (l.amount ?? []).length === 0) return 0;
    const nowTs = new Date();
    // Filter out any future-dated entries before finding the latest
    const past = (l.amount ?? []).filter((e) => new Date(e.date ?? '') <= nowTs);
    if (past.length === 0) return 0;
    // Pick the entry with the most-recent date using a reduce comparison
    return (
      past.reduce((latest, e) =>
        new Date(e.date ?? '') > new Date(latest.date ?? '') ? e : latest,
      ).value ?? 0
    );
  }

  // Returns the current price of the asset that is open in the modal (for the "current:" hint)
  function selectedAssetCurrentPrice(): number {
    return assets.find((a) => a.id === selectedAssetId)?.currentPrice ?? 0;
  }

  // Returns the current balance of the liability open in the modal (for the "current:" hint)
  function selectedLiabilityCurrentBalance(): number {
    return selectedLiabilityId ? currentLiabilityBalance(selectedLiabilityId) : 0;
  }

  // ── Render ─────────────────────────────────────────────────────────────

  if (loading)
    return (
      <div className="loading-center gap-2">
        <div className="spinner-border spinner-border-sm text-primary" role="status" />
        <span className="text-muted">Loading portfolio…</span>
      </div>
    );

  if (error)
    return (
      <div className="alert alert-danger mt-4">
        <strong>Failed to load portfolio:</strong> {error}
      </div>
    );

  const nw = netWorth ?? { totalAssets: 0, totalLiabilities: 0, netWorth: 0 };
  const gl = globalGainLoss;

  return (
    <div>
      {/* ── Page header ── */}
      <div className="page-header d-flex justify-content-between align-items-start">
        <div>
          <h1 className="page-title">Investment Portfolio</h1>
          <p className="page-subtitle">Track assets, liabilities and performance over time.</p>
        </div>
        <span className="text-muted small mt-1">
          {new Date().toLocaleDateString('en-GB', {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
          })}
        </span>
      </div>

      {/* ── Summary cards — single row, no duplication ── */}
      <div className="row g-3 mb-4">
        {/* Net Worth — featured */}
        <div className="col-12 col-md-6 col-lg-3">
          <div className="card pf-metric pf-metric-networth h-100">
            <div className="card-body">
              <div className="pf-metric-label">Net Worth</div>
              <div
                className={`pf-metric-value featured ${(nw.netWorth ?? 0) >= 0 ? 'text-primary' : 'text-danger'}`}
              >
                {fmt(nw.netWorth ?? 0)}
              </div>
              <div className="pf-metric-sub">Assets − Liabilities</div>
            </div>
          </div>
        </div>

        <div className="col-6 col-lg-3">
          <div className="card pf-metric pf-metric-assets h-100">
            <div className="card-body">
              <div className="pf-metric-label">Total Assets</div>
              <div className="pf-metric-value" style={{ color: '#2563eb' }}>
                {fmt(nw.totalAssets ?? 0)}
              </div>
              <div className="pf-metric-sub">
                {assets.length} position{assets.length !== 1 ? 's' : ''}
              </div>
            </div>
          </div>
        </div>

        <div className="col-6 col-lg-3">
          <div className="card pf-metric pf-metric-liabilities h-100">
            <div className="card-body">
              <div className="pf-metric-label">Total Liabilities</div>
              <div className="pf-metric-value text-danger">{fmt(nw.totalLiabilities ?? 0)}</div>
              <div className="pf-metric-sub">
                {liabilities.length} item{liabilities.length !== 1 ? 's' : ''}
              </div>
            </div>
          </div>
        </div>

        <div className="col-12 col-md-6 col-lg-3">
          <div
            className={`card pf-metric ${!gl || (gl.totalGainLoss ?? 0) >= 0 ? 'pf-metric-gain' : 'pf-metric-loss'} h-100`}
          >
            <div className="card-body">
              <div className="pf-metric-label">Unrealised G/L</div>
              {gl ? (
                <>
                  <div
                    className={`pf-metric-value ${(gl.totalGainLoss ?? 0) >= 0 ? 'text-success' : 'text-danger'}`}
                  >
                    {(gl.totalGainLoss ?? 0) >= 0 ? '+' : ''}
                    {fmt(gl.totalGainLoss ?? 0)}
                  </div>
                  <div
                    className={`pf-metric-sub ${(gl.totalGainLoss ?? 0) >= 0 ? 'text-success' : 'text-danger'}`}
                  >
                    {(gl.totalGainLossPercent ?? 0) >= 0 ? '+' : ''}
                    {(gl.totalGainLossPercent ?? 0).toFixed(2)}% · cost {fmt(gl.totalInvested ?? 0)}
                  </div>
                </>
              ) : (
                <div className="pf-metric-value text-muted">—</div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ── Allocation ── */}
      {allocation.length > 0 && (
        <div className="card pf-alloc-card mb-4">
          <div className="card-body">
            <div className="pf-section-label">Allocation by Type</div>
            {allocation.map((a) => (
              <div key={a.type} className="pf-alloc-row">
                <div className="pf-alloc-type">
                  <span className="pf-alloc-dot" style={{ background: allocColor(a.type ?? '') }} />
                  {a.type}
                </div>
                <div className="pf-alloc-bar-wrap">
                  <div
                    className="pf-alloc-bar"
                    style={{
                      width: `${a.allocationPercent ?? 0}%`,
                      background: allocColor(a.type ?? ''),
                    }}
                  />
                </div>
                <div className="pf-alloc-pct" style={{ color: allocColor(a.type ?? '') }}>
                  {(a.allocationPercent ?? 0).toFixed(1)}%
                </div>
                <div className="pf-alloc-val">{fmt(a.totalValue ?? 0)}</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── Tabs ── */}
      <ul className="nav nav-tabs mb-0">
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${tab === 'assets' ? 'active' : ''}`}
            onClick={() => switchTab('assets')}
          >
            Assets{' '}
            <span className="badge bg-secondary ms-1" style={{ fontSize: '0.65rem' }}>
              {assets.length}
            </span>
          </button>
        </li>
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${tab === 'liabilities' ? 'active' : ''}`}
            onClick={() => switchTab('liabilities')}
          >
            Liabilities{' '}
            <span className="badge bg-secondary ms-1" style={{ fontSize: '0.65rem' }}>
              {liabilities.length}
            </span>
          </button>
        </li>
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${tab === 'performance' ? 'active' : ''}`}
            onClick={() => switchTab('performance')}
          >
            Performance
          </button>
        </li>
        <li className="nav-item">
          <button
            type="button"
            className={`nav-link ${tab === 'networth' ? 'active' : ''}`}
            onClick={() => switchTab('networth')}
          >
            Net Worth History
          </button>
        </li>
      </ul>

      {/* ── Assets tab ── */}
      {tab === 'assets' && (
        <div className="card pf-tab-card">
          <div className="card-body">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="fw-semibold" style={{ fontSize: '0.9rem' }}>
                Investment Assets
              </span>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={openCreateAsset}
                style={{ borderRadius: 8 }}
              >
                + Add Asset
              </button>
            </div>

            {assets.length === 0 ? (
              <div className="pf-empty">
                <div className="pf-empty-icon">📊</div>
                <div className="pf-empty-title">No assets yet</div>
                <div className="pf-empty-sub">
                  Click <strong>+ Add Asset</strong> to record your first investment.
                </div>
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table pf-table mb-0">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Type</th>
                      <th className="text-end">Qty</th>
                      <th className="text-end">Purchase</th>
                      <th className="text-end">Current</th>
                      <th className="text-end">Value</th>
                      <th className="text-end">Gain / Loss</th>
                      <th />
                    </tr>
                  </thead>
                  <tbody>
                    {assets.map((a) => (
                      <tr key={a.id}>
                        <td className="fw-semibold">{a.name}</td>
                        <td>
                          <TypeBadge type={a.type ?? ''} />
                        </td>
                        <td className="text-end">{a.quantity}</td>
                        <td className="text-end text-muted">{fmt(a.purchasePrice ?? 0)}</td>
                        <td className="text-end fw-semibold">{fmt(a.currentPrice ?? 0)}</td>
                        <td className="text-end fw-bold">{fmt(a.currentValue ?? 0)}</td>
                        <td className="text-end">
                          <GainBadge
                            value={a.unrealisedGainLoss ?? 0}
                            percent={a.unrealisedGainLossPercent ?? 0}
                          />
                        </td>
                        <td>
                          <div className="pf-btn-group">
                            <button
                              type="button"
                              className="pf-btn-icon price"
                              onClick={() => openAddPrice(a.id ?? '')}
                              title="Update price"
                            >
                              ↑
                            </button>
                            <button
                              type="button"
                              className="pf-btn-icon edit"
                              onClick={() => openEditAsset(a.id ?? '')}
                              title="Edit"
                            >
                              ✎
                            </button>
                            <button
                              type="button"
                              className="pf-btn-icon danger"
                              onClick={() => {
                                setDeleteError('');
                                setDeleteTarget({
                                  type: 'asset',
                                  id: a.id ?? '',
                                  name: a.name ?? '',
                                });
                              }}
                              title="Delete"
                            >
                              ✕
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
      )}

      {/* ── Liabilities tab ── */}
      {tab === 'liabilities' && (
        <div className="card pf-tab-card">
          <div className="card-body">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="fw-semibold" style={{ fontSize: '0.9rem' }}>
                Liabilities
              </span>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={openCreateLiability}
                style={{ borderRadius: 8 }}
              >
                + Add Liability
              </button>
            </div>

            {liabilities.length === 0 ? (
              <div className="pf-empty">
                <div className="pf-empty-icon">📋</div>
                <div className="pf-empty-title">No liabilities recorded</div>
                <div className="pf-empty-sub">
                  Click <strong>+ Add Liability</strong> to track debts or loans.
                </div>
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table pf-table mb-0">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Type</th>
                      <th className="text-end">Current Balance</th>
                      <th />
                    </tr>
                  </thead>
                  <tbody>
                    {liabilities.map((l) => (
                      <tr key={l.id}>
                        <td className="fw-semibold">{l.name}</td>
                        <td>
                          <TypeBadge type={l.type ?? ''} />
                        </td>
                        <td className="text-end fw-bold text-danger">
                          {fmt(currentLiabilityBalance(l.id ?? ''))}
                        </td>
                        <td>
                          <div className="pf-btn-group">
                            <button
                              type="button"
                              className="pf-btn-icon price"
                              onClick={() => openAddAmount(l.id ?? '')}
                              title="Update balance"
                            >
                              ↑
                            </button>
                            <button
                              type="button"
                              className="pf-btn-icon edit"
                              onClick={() => openEditLiability(l.id ?? '')}
                              title="Edit"
                            >
                              ✎
                            </button>
                            <button
                              type="button"
                              className="pf-btn-icon danger"
                              onClick={() => {
                                setDeleteError('');
                                setDeleteTarget({
                                  type: 'liability',
                                  id: l.id ?? '',
                                  name: l.name ?? '',
                                });
                              }}
                              title="Delete"
                            >
                              ✕
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
      )}

      {/* ── Performance tab ── */}
      {tab === 'performance' && (
        <div className="card pf-tab-card">
          <div className="card-body">
            <div className="pf-perf-controls">
              <div>
                <p className="form-label small fw-semibold mb-1">From</p>
                <MonthPicker value={perfFrom} onChange={setPerfFrom} max={perfTo} />
              </div>
              <div>
                <p className="form-label small fw-semibold mb-1">To</p>
                <MonthPicker value={perfTo} onChange={setPerfTo} min={perfFrom} />
              </div>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={loadPerformance}
                disabled={perfLoading}
                style={{ borderRadius: 8 }}
              >
                {perfLoading ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-1" />
                    Loading…
                  </>
                ) : (
                  'Reload'
                )}
              </button>
            </div>

            {perfLoading && (
              <div className="d-flex justify-content-center py-4">
                <div className="spinner-border spinner-border-sm text-primary" />
              </div>
            )}

            {!perfLoading && monthlyPerf.length === 0 && perfLoaded && (
              <div className="pf-empty">
                <div className="pf-empty-icon">📉</div>
                <div className="pf-empty-title">No data for this period</div>
                <div className="pf-empty-sub">Try a wider date range.</div>
              </div>
            )}

            {!perfLoading && monthlyPerf.length > 0 && (
              <div className="table-responsive">
                <table className="table pf-table mb-0">
                  <thead>
                    <tr>
                      <th>Month</th>
                      <th className="text-end">Start Value</th>
                      <th className="text-end">End Value</th>
                      <th className="text-end">Gain / Loss</th>
                      <th className="text-end">%</th>
                    </tr>
                  </thead>
                  <tbody>
                    {monthlyPerf.map((m) => (
                      <tr key={m.month}>
                        <td className="fw-semibold">{m.month}</td>
                        <td className="text-end text-muted">{fmt(m.startValue ?? 0)}</td>
                        <td className="text-end">{fmt(m.endValue ?? 0)}</td>
                        <td
                          className={`text-end fw-semibold ${(m.gainLoss ?? 0) >= 0 ? 'text-success' : 'text-danger'}`}
                        >
                          {(m.gainLoss ?? 0) >= 0 ? '+' : ''}
                          {fmt(m.gainLoss ?? 0)}
                        </td>
                        <td
                          className={`text-end ${(m.gainLoss ?? 0) >= 0 ? 'text-success' : 'text-danger'}`}
                        >
                          {(m.gainLossPercent ?? 0) >= 0 ? '+' : ''}
                          {(m.gainLossPercent ?? 0).toFixed(2)}%
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr style={{ borderTop: '2px solid #e5e7eb', fontWeight: 700 }}>
                      <td>Total</td>
                      <td />
                      <td />
                      <td
                        className={`text-end ${monthlyPerf.reduce((s, m) => s + (m.gainLoss ?? 0), 0) >= 0 ? 'text-success' : 'text-danger'}`}
                      >
                        {(() => {
                          const t = monthlyPerf.reduce((s, m) => s + (m.gainLoss ?? 0), 0);
                          return `${t >= 0 ? '+' : ''}${fmt(t)}`;
                        })()}
                      </td>
                      <td />
                    </tr>
                  </tfoot>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ── Net Worth History tab ── */}
      {tab === 'networth' && (
        <div className="card pf-tab-card">
          <div className="card-body">
            {/* Date range controls */}
            <div className="pf-perf-controls mb-4">
              <div>
                <p className="form-label small fw-semibold mb-1">From</p>
                <DatePicker value={nwFrom} onChange={setNwFrom} max={nwTo} />
              </div>
              <div>
                <p className="form-label small fw-semibold mb-1">To</p>
                <DatePicker value={nwTo} onChange={setNwTo} min={nwFrom} max={today()} />
              </div>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={loadNwHistory}
                disabled={nwLoading}
                style={{ borderRadius: 8 }}
              >
                {nwLoading ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-1" />
                    Loading…
                  </>
                ) : (
                  'Load'
                )}
              </button>
            </div>

            {nwLoading && (
              <div className="loading-center">
                <div className="spinner-border spinner-border-sm text-primary" />
              </div>
            )}

            {!nwLoading && nwLoaded && nwHistory.length === 0 && (
              <div className="pf-empty">
                <div className="pf-empty-icon">📈</div>
                <div className="pf-empty-title">No data for this period</div>
                <div className="pf-empty-sub">Try a wider date range or add some assets.</div>
              </div>
            )}

            {!nwLoading && nwHistory.length > 0 && (
              <>
                {/* Chart */}
                <ResponsiveContainer width="100%" height={320}>
                  <AreaChart data={nwHistory} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
                    <defs>
                      <linearGradient id="nw-assets" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#2563eb" stopOpacity={0.15} />
                        <stop offset="95%" stopColor="#2563eb" stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="nw-liab" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#dc2626" stopOpacity={0.12} />
                        <stop offset="95%" stopColor="#dc2626" stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="nw-net" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#16a34a" stopOpacity={0.18} />
                        <stop offset="95%" stopColor="#16a34a" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
                    <XAxis
                      dataKey="date"
                      tickFormatter={(v: string) => {
                        const d = new Date(v);
                        return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
                      }}
                      tick={{ fontSize: 11, fill: '#94a3b8' }}
                      axisLine={false}
                      tickLine={false}
                      minTickGap={40}
                    />
                    <YAxis
                      tickFormatter={(v: number) => fmt(v)}
                      tick={{ fontSize: 11, fill: '#94a3b8' }}
                      axisLine={false}
                      tickLine={false}
                      width={80}
                    />
                    <Tooltip
                      formatter={(value, name) => [
                        fmt(Number(value)),
                        name === 'totalAssets'
                          ? 'Assets'
                          : name === 'totalLiabilities'
                            ? 'Liabilities'
                            : 'Net Worth',
                      ]}
                      labelFormatter={(label) =>
                        new Date(String(label)).toLocaleDateString('en-GB', {
                          day: 'numeric',
                          month: 'long',
                          year: 'numeric',
                        })
                      }
                      contentStyle={{
                        background: '#fff',
                        border: '1px solid #e2e8f0',
                        borderRadius: 8,
                        fontSize: 13,
                        boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
                      }}
                    />
                    <Area
                      type="monotone"
                      dataKey="totalAssets"
                      stroke="#2563eb"
                      strokeWidth={1.5}
                      fill="url(#nw-assets)"
                      dot={false}
                    />
                    <Area
                      type="monotone"
                      dataKey="totalLiabilities"
                      stroke="#dc2626"
                      strokeWidth={1.5}
                      fill="url(#nw-liab)"
                      dot={false}
                    />
                    <Area
                      type="monotone"
                      dataKey="netWorth"
                      stroke="#16a34a"
                      strokeWidth={2}
                      fill="url(#nw-net)"
                      dot={false}
                    />
                  </AreaChart>
                </ResponsiveContainer>

                {/* Legend */}
                <div className="d-flex gap-4 justify-content-center mt-3">
                  {[
                    { color: '#16a34a', label: 'Net Worth' },
                    { color: '#2563eb', label: 'Total Assets' },
                    { color: '#dc2626', label: 'Liabilities' },
                  ].map(({ color, label }) => (
                    <div key={label} className="d-flex align-items-center gap-2">
                      <div style={{ width: 12, height: 3, background: color, borderRadius: 2 }} />
                      <span style={{ fontSize: '0.78rem', color: '#64748b' }}>{label}</span>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>
      )}

      {/* ── Asset modal ── */}
      {assetModal && (
        <div
          className="pf-modal-overlay"
          onClick={() => setAssetModal(null)}
          onKeyDown={(e) => e.key === 'Escape' && setAssetModal(null)}
        >
          <div
            className="pf-modal"
            onClick={(e) => e.stopPropagation()}
            onKeyDown={(e) => e.stopPropagation()}
          >
            <div className="pf-modal-header">
              <h5 className="pf-modal-title">
                {assetModal === 'create' && '+ Add Asset'}
                {assetModal === 'edit' && 'Edit Asset'}
                {assetModal === 'price' && 'Update Price'}
              </h5>
              <button type="button" className="btn-close" onClick={() => setAssetModal(null)} />
            </div>
            <div className="pf-modal-body">
              {(assetModal === 'create' || assetModal === 'edit') && (
                <>
                  <div className="mb-3">
                    <p className="form-label small fw-semibold">Name</p>
                    <input
                      className={`form-control ${assetValidation.name ? 'is-invalid' : ''}`}
                      value={assetForm.name}
                      onChange={(e) => setAssetForm((f) => ({ ...f, name: e.target.value }))}
                      placeholder="e.g. Apple Inc."
                      // biome-ignore lint/a11y/noAutofocus: first input in modal
                      autoFocus
                    />
                    {assetValidation.name && (
                      <div className="invalid-feedback">{assetValidation.name}</div>
                    )}
                  </div>
                  <div className="mb-3">
                    <p className="form-label small fw-semibold">Type</p>
                    <select
                      className="form-select"
                      value={assetForm.type}
                      onChange={(e) => setAssetForm((f) => ({ ...f, type: e.target.value }))}
                    >
                      {assetTypes.map((t) => (
                        <option key={t}>{t}</option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-3">
                    <p className="form-label small fw-semibold">Quantity</p>
                    <input
                      type="number"
                      min="0"
                      step="any"
                      className={`form-control ${assetValidation.quantity ? 'is-invalid' : ''}`}
                      value={assetForm.quantity}
                      onChange={(e) => setAssetForm((f) => ({ ...f, quantity: e.target.value }))}
                      placeholder="e.g. 10"
                    />
                    {assetValidation.quantity && (
                      <div className="invalid-feedback">{assetValidation.quantity}</div>
                    )}
                  </div>
                  {assetModal === 'create' && (
                    <>
                      <div className="mb-3">
                        <p className="form-label small fw-semibold">
                          Purchase Price <span className="fw-normal text-muted">(per unit)</span>
                        </p>
                        <div className="input-group">
                          <span className="input-group-text">€</span>
                          <input
                            type="number"
                            min="0"
                            step="any"
                            className={`form-control ${assetValidation.purchasePrice ? 'is-invalid' : ''}`}
                            value={assetForm.purchasePrice}
                            onChange={(e) =>
                              setAssetForm((f) => ({ ...f, purchasePrice: e.target.value }))
                            }
                            placeholder="0.00"
                          />
                          {assetValidation.purchasePrice && (
                            <div className="invalid-feedback">{assetValidation.purchasePrice}</div>
                          )}
                        </div>
                      </div>
                      <div className="mb-0">
                        <p className="form-label small fw-semibold">Purchase Date</p>
                        <DatePicker
                          value={assetForm.purchaseDate}
                          onChange={(v) => setAssetForm((f) => ({ ...f, purchaseDate: v }))}
                          max={today()}
                          invalid={!!assetValidation.purchaseDate}
                          dropUp
                        />
                        {assetValidation.purchaseDate ? (
                          <div className="text-danger small mt-1">
                            {assetValidation.purchaseDate}
                          </div>
                        ) : (
                          <div className="form-text">Cannot be a future date.</div>
                        )}
                      </div>
                    </>
                  )}
                </>
              )}
              {assetModal === 'price' && (
                <>
                  {selectedAssetCurrentPrice() > 0 && (
                    <div className="pf-current-hint">
                      Current price: <strong>{fmt(selectedAssetCurrentPrice())}</strong>
                    </div>
                  )}
                  <div className="mb-3">
                    <p className="form-label small fw-semibold">New Price</p>
                    <div className="input-group">
                      <span className="input-group-text">€</span>
                      <input
                        type="number"
                        min="0"
                        step="any"
                        className={`form-control ${assetValidation.value ? 'is-invalid' : ''}`}
                        value={priceForm.value}
                        onChange={(e) => setPriceForm((f) => ({ ...f, value: e.target.value }))}
                        placeholder="0.00"
                        // biome-ignore lint/a11y/noAutofocus: first input in modal
                        autoFocus
                      />
                      {assetValidation.value && (
                        <div className="invalid-feedback">{assetValidation.value}</div>
                      )}
                    </div>
                  </div>
                  <div className="mb-0">
                    <p className="form-label small fw-semibold">As of Date</p>
                    <DatePicker
                      value={priceForm.date}
                      onChange={(v) => setPriceForm((f) => ({ ...f, date: v }))}
                      max={today()}
                      invalid={!!assetValidation.date}
                      dropUp
                    />
                    {assetValidation.date ? (
                      <div className="text-danger small mt-1">{assetValidation.date}</div>
                    ) : (
                      <div className="form-text">Cannot be a future date.</div>
                    )}
                  </div>
                </>
              )}
            </div>
            <div className="pf-modal-footer">
              {assetError && <div className="alert alert-danger py-2 small mb-0">{assetError}</div>}
              <div className="pf-modal-footer-actions">
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => setAssetModal(null)}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn btn-primary btn-sm"
                  onClick={submitAsset}
                  disabled={assetSaving}
                >
                  {assetSaving ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-1" />
                      Saving…
                    </>
                  ) : (
                    'Save'
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── Liability modal ── */}
      {liabilityModal && (
        <div
          className="pf-modal-overlay"
          onClick={() => setLiabilityModal(null)}
          onKeyDown={(e) => e.key === 'Escape' && setLiabilityModal(null)}
        >
          <div
            className="pf-modal"
            onClick={(e) => e.stopPropagation()}
            onKeyDown={(e) => e.stopPropagation()}
          >
            <div className="pf-modal-header">
              <h5 className="pf-modal-title">
                {liabilityModal === 'create' && '+ Add Liability'}
                {liabilityModal === 'edit' && 'Edit Liability'}
                {liabilityModal === 'amount' && 'Update Balance'}
              </h5>
              <button type="button" className="btn-close" onClick={() => setLiabilityModal(null)} />
            </div>
            <div className="pf-modal-body">
              {(liabilityModal === 'create' || liabilityModal === 'edit') && (
                <>
                  <div className="mb-3">
                    <p className="form-label small fw-semibold">Name</p>
                    <input
                      className={`form-control ${liabilityValidation.name ? 'is-invalid' : ''}`}
                      value={liabilityForm.name}
                      onChange={(e) => setLiabilityForm((f) => ({ ...f, name: e.target.value }))}
                      placeholder="e.g. Home Mortgage"
                      // biome-ignore lint/a11y/noAutofocus: first input in modal
                      autoFocus
                    />
                    {liabilityValidation.name && (
                      <div className="invalid-feedback">{liabilityValidation.name}</div>
                    )}
                  </div>
                  <div className="mb-3">
                    <p className="form-label small fw-semibold">Type</p>
                    <select
                      className="form-select"
                      value={liabilityForm.type}
                      onChange={(e) => setLiabilityForm((f) => ({ ...f, type: e.target.value }))}
                    >
                      {liabilityTypes.map((t) => (
                        <option key={t}>{t}</option>
                      ))}
                    </select>
                  </div>
                  {liabilityModal === 'create' && (
                    <>
                      <div className="mb-3">
                        <p className="form-label small fw-semibold">Initial Balance</p>
                        <div className="input-group">
                          <span className="input-group-text">€</span>
                          <input
                            type="number"
                            min="0"
                            step="any"
                            className={`form-control ${liabilityValidation.initialAmount ? 'is-invalid' : ''}`}
                            value={liabilityForm.initialAmount}
                            onChange={(e) =>
                              setLiabilityForm((f) => ({ ...f, initialAmount: e.target.value }))
                            }
                            placeholder="0.00"
                          />
                          {liabilityValidation.initialAmount && (
                            <div className="invalid-feedback">
                              {liabilityValidation.initialAmount}
                            </div>
                          )}
                        </div>
                      </div>
                      <div className="mb-0">
                        <p className="form-label small fw-semibold">Date</p>
                        <DatePicker
                          value={liabilityForm.date}
                          onChange={(v) => setLiabilityForm((f) => ({ ...f, date: v }))}
                          max={today()}
                          invalid={!!liabilityValidation.date}
                          dropUp
                        />
                        {liabilityValidation.date ? (
                          <div className="text-danger small mt-1">{liabilityValidation.date}</div>
                        ) : (
                          <div className="form-text">Cannot be a future date.</div>
                        )}
                      </div>
                    </>
                  )}
                </>
              )}
              {liabilityModal === 'amount' && (
                <>
                  {selectedLiabilityCurrentBalance() > 0 && (
                    <div className="pf-current-hint">
                      Current balance: <strong>{fmt(selectedLiabilityCurrentBalance())}</strong>
                    </div>
                  )}
                  <div className="mb-3">
                    <p className="form-label small fw-semibold">New Balance</p>
                    <div className="input-group">
                      <span className="input-group-text">€</span>
                      <input
                        type="number"
                        min="0"
                        step="any"
                        className={`form-control ${liabilityValidation.value ? 'is-invalid' : ''}`}
                        value={amountForm.value}
                        onChange={(e) => setAmountForm((f) => ({ ...f, value: e.target.value }))}
                        placeholder="0.00"
                        // biome-ignore lint/a11y/noAutofocus: first input in modal
                        autoFocus
                      />
                      {liabilityValidation.value && (
                        <div className="invalid-feedback">{liabilityValidation.value}</div>
                      )}
                    </div>
                  </div>
                  <div className="mb-0">
                    <p className="form-label small fw-semibold">As of Date</p>
                    <DatePicker
                      value={amountForm.date}
                      onChange={(v) => setAmountForm((f) => ({ ...f, date: v }))}
                      max={today()}
                      invalid={!!liabilityValidation.date}
                      dropUp
                    />
                    {liabilityValidation.date ? (
                      <div className="text-danger small mt-1">{liabilityValidation.date}</div>
                    ) : (
                      <div className="form-text">Cannot be a future date.</div>
                    )}
                  </div>
                </>
              )}
            </div>
            <div className="pf-modal-footer">
              {liabilityError && (
                <div className="alert alert-danger py-2 small mb-0">{liabilityError}</div>
              )}
              <div className="pf-modal-footer-actions">
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => setLiabilityModal(null)}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn btn-primary btn-sm"
                  onClick={submitLiability}
                  disabled={liabilitySaving}
                >
                  {liabilitySaving ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-1" />
                      Saving…
                    </>
                  ) : (
                    'Save'
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── Delete confirmation ── */}
      {deleteTarget && (
        <div
          className="pf-modal-overlay"
          onClick={() => setDeleteTarget(null)}
          onKeyDown={(e) => e.key === 'Escape' && setDeleteTarget(null)}
        >
          <div
            className="pf-modal pf-modal-sm pf-modal-danger"
            onClick={(e) => e.stopPropagation()}
            onKeyDown={(e) => e.stopPropagation()}
          >
            <div className="pf-modal-header">
              <h5 className="pf-modal-title">
                Delete {deleteTarget.type === 'asset' ? 'Asset' : 'Liability'}
              </h5>
              <button type="button" className="btn-close" onClick={() => setDeleteTarget(null)} />
            </div>
            <div className="pf-modal-body">
              <p className="mb-0 text-muted" style={{ fontSize: '0.9rem' }}>
                Are you sure you want to delete{' '}
                <strong style={{ color: 'inherit' }}>{deleteTarget.name}</strong>? This cannot be
                undone.
              </p>
              {deleteError && (
                <div className="alert alert-danger py-2 small mt-3 mb-0">{deleteError}</div>
              )}
            </div>
            <div className="pf-modal-footer">
              <div className="pf-modal-footer-actions">
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => setDeleteTarget(null)}
                >
                  Cancel
                </button>
                <button type="button" className="btn btn-danger btn-sm" onClick={confirmDelete}>
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
