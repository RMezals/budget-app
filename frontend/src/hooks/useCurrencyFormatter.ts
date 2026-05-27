import { useCurrency } from '@/contexts/CurrencyContext';
import { formatCurrency } from '@/utils/currency/formatter';
import { useCallback } from 'react';

// Returns a stable formatting function bound to the current user's currency preference.
// Components can call fmt(123.45) without having to pass the currency code themselves.
export function useCurrencyFormatter() {
  const { currency } = useCurrency();

  // useCallback keeps the same function reference across renders unless currency changes,
  // preventing unnecessary re-renders in child components that receive fmt as a prop
  const format = useCallback(
    (value: number): string => {
      return formatCurrency(value, currency);
    },
    [currency], // Re-create the formatter only when the user switches currency
  );

  return format;
}
