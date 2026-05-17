import { useCurrency } from '@/contexts/CurrencyContext';
import { formatCurrency } from '@/utils/currency/formatter';
import { useCallback } from 'react';

/**
 * Hook that provides a currency formatter function using the current user's currency
 *
 * @returns Formatter function that accepts a number and returns formatted currency string
 */
export function useCurrencyFormatter() {
  const { currency } = useCurrency();

  const format = useCallback(
    (value: number): string => {
      return formatCurrency(value, currency);
    },
    [currency],
  );

  return format;
}
