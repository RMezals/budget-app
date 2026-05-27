import { apiFetch } from '@/api/client';
import type { AssetAllocation, AssetSummary, Liability, NetWorthSnapshot } from '@/api/types';
import { useCallback, useEffect, useState } from 'react';

// All portfolio data is grouped into a single state object so one update re-renders once
interface PortfolioData {
  netWorth: NetWorthSnapshot | null;
  assets: AssetSummary[];
  allocation: AssetAllocation[];
  liabilities: Liability[];
  assetTypes: string[];
  liabilityTypes: string[];
  loading: boolean;
  error: string | null;
}

// Loads and exposes all portfolio data — assets, liabilities, allocation, and net worth
export function usePortfolio() {
  const [data, setData] = useState<PortfolioData>({
    netWorth: null,
    assets: [],
    allocation: [],
    liabilities: [],
    assetTypes: [],
    liabilityTypes: [],
    loading: true,
    error: null,
  });

  // Fetches all six portfolio endpoints in parallel for speed, then stores them together
  const load = useCallback(async () => {
    // Mark as loading while keeping existing data visible during refresh
    setData((d) => ({ ...d, loading: true, error: null }));
    try {
      // Promise.all runs all requests simultaneously instead of sequentially
      const [netWorth, assets, allocation, liabilities, assetTypes, liabilityTypes] =
        await Promise.all([
          apiFetch<NetWorthSnapshot>('/api/networth'),
          apiFetch<AssetSummary[]>('/api/assets/summary'),
          apiFetch<AssetAllocation[]>('/api/assets/allocation'),
          apiFetch<Liability[]>('/api/liabilities'),
          apiFetch<string[]>('/api/assets/types'),
          apiFetch<string[]>('/api/liabilities/types'),
        ]);
      setData({
        netWorth,
        assets,
        allocation,
        liabilities,
        assetTypes,
        liabilityTypes,
        loading: false,
        error: null,
      });
    } catch (e) {
      // Keep existing data visible and surface the error message
      setData((d) => ({ ...d, loading: false, error: String(e) }));
    }
  }, []); // No dependencies — load is stable and never needs to be re-created

  // Trigger the initial data fetch when the hook is first used
  useEffect(() => {
    load();
  }, [load]);

  // Expose reload so the page can refresh data after creating or deleting an asset/liability
  return { ...data, reload: load };
}
