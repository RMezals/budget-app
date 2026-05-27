import { apiFetch } from '@/api/client';
import { SpendingTrendSchema } from '@/api/schemas';
import type { SpendingTrendPoint } from '@/api/types';
import { useEffect, useState } from 'react';

// Fetches spending trend data for the given number of past months from the backend
export function useSpendingTrend(months = 12) {
  const [data, setData] = useState<SpendingTrendPoint[]>([]);
  const [loading, setLoading] = useState(true);

  // Re-fetches whenever the months parameter changes (e.g. user switches time range)
  useEffect(() => {
    // cancelled flag prevents state updates if the component unmounts before the fetch finishes
    let cancelled = false;
    setLoading(true);
    apiFetch(`/api/dashboard/spending-trend?months=${months}`, SpendingTrendSchema)
      .then((d) => {
        // Only update state if this effect is still active
        if (!cancelled) setData(d);
      })
      .catch(() => {}) // Silently ignore errors — the chart will just remain empty
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    // Cleanup: mark the in-flight request as stale when months changes or component unmounts
    return () => {
      cancelled = true;
    };
  }, [months]);

  return { data, loading };
}
