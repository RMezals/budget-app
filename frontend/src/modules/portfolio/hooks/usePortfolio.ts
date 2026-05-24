import { apiFetch } from '@/api/client';
import type { AssetAllocation, AssetSummary, Liability, NetWorthSnapshot } from '@/api/types';
import { useCallback, useEffect, useState } from 'react';

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

  const load = useCallback(async () => {
    setData((d) => ({ ...d, loading: true, error: null }));
    try {
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
      setData((d) => ({ ...d, loading: false, error: String(e) }));
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return { ...data, reload: load };
}
