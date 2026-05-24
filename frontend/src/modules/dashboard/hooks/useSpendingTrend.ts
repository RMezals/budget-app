import { apiFetch } from '@/api/client';
import { SpendingTrendSchema } from '@/api/schemas';
import type { SpendingTrendPoint } from '@/api/types';
import { useEffect, useState } from 'react';

export function useSpendingTrend(months = 12) {
  const [data, setData] = useState<SpendingTrendPoint[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    apiFetch(`/api/dashboard/spending-trend?months=${months}`, SpendingTrendSchema)
      .then((d) => {
        if (!cancelled) setData(d);
      })
      .catch(() => {})
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [months]);

  return { data, loading };
}
