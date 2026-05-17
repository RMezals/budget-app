import type { CurrencyCode } from '@/utils/currency/constants';

const formatters = new Map<CurrencyCode, Intl.NumberFormat>();

function getFormatter(currencyCode: CurrencyCode): Intl.NumberFormat {
  let formatter = formatters.get(currencyCode);
  if (!formatter) {
    formatter = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currencyCode,
    });
    formatters.set(currencyCode, formatter);
  }
  return formatter;
}

/**
 * Formats a number as currency with the specified currency code
 *
 * @param value - The numeric value to format
 * @param currencyCode - ISO 4217 currency code (e.g., 'USD', 'EUR')
 * @returns Formatted currency string
 */
export function formatCurrency(value: number, currencyCode: CurrencyCode): string {
  return getFormatter(currencyCode).format(value);
}
