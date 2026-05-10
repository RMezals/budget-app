/**
 * Supported currency codes
 */
export const SUPPORTED_CURRENCIES = ['USD', 'EUR', 'GBP', 'JPY', 'CAD', 'AUD'] as const;

export type CurrencyCode = (typeof SUPPORTED_CURRENCIES)[number];

/**
 * Default currency used when Firebase is not configured or claim is missing
 */
export const DEFAULT_CURRENCY: CurrencyCode = 'EUR';

/**
 * Firebase custom claim key for user's preferred currency
 */
export const CURRENCY_CLAIM_KEY = 'currency';
