import type { CurrencyCode } from '@/utils/currency/constants';

// Cache of Intl.NumberFormat instances — creating these is expensive, so we reuse them
const formatters = new Map<CurrencyCode, Intl.NumberFormat>();

// Returns a cached formatter for the given currency, creating one on first use
function getFormatter(currencyCode: CurrencyCode): Intl.NumberFormat {
  let formatter = formatters.get(currencyCode);
  if (!formatter) {
    // en-US locale is used for consistent number grouping (e.g. 1,234.56)
    formatter = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currencyCode,
    });
    // Store in the module-level Map so subsequent calls skip the constructor
    formatters.set(currencyCode, formatter);
  }
  return formatter;
}

// Formats a number as a localised currency string (e.g. 1234.5 → "$1,234.50")
export function formatCurrency(value: number, currencyCode: CurrencyCode): string {
  return getFormatter(currencyCode).format(value);
}
