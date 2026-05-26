import {
  CURRENCY_CLAIM_KEY,
  DEFAULT_CURRENCY,
  SUPPORTED_CURRENCIES,
} from '@/utils/currency/constants';
import type { CurrencyCode } from '@/utils/currency/constants';
import type { User } from 'firebase/auth';

/**
 * Extracts currency code from Firebase user token claims
 *
 * @param user - Firebase user object (may be null if not authenticated)
 * @returns Currency code from token claims, or default currency
 */
export async function getCurrencyFromToken(
  user: User | null,
  forceRefresh = false,
): Promise<CurrencyCode> {
  if (!user) {
    return DEFAULT_CURRENCY;
  }

  try {
    const tokenResult = await user.getIdTokenResult(forceRefresh);
    const currencyClaim = tokenResult.claims[CURRENCY_CLAIM_KEY];

    if (isSupportedCurrency(currencyClaim)) {
      return currencyClaim;
    }

    return DEFAULT_CURRENCY;
  } catch (error) {
    console.warn('Failed to extract currency from token, using default:', error);
    return DEFAULT_CURRENCY;
  }
}

function isSupportedCurrency(value: unknown): value is CurrencyCode {
  return typeof value === 'string' && SUPPORTED_CURRENCIES.includes(value as CurrencyCode);
}
