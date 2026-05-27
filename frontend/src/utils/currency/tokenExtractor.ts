import {
  CURRENCY_CLAIM_KEY,
  DEFAULT_CURRENCY,
  SUPPORTED_CURRENCIES,
} from '@/utils/currency/constants';
import type { CurrencyCode } from '@/utils/currency/constants';
import type { User } from 'firebase/auth';

// Reads the user's preferred currency from their Firebase ID token custom claims.
// The backend sets the currency claim when the user updates their profile.
export async function getCurrencyFromToken(
  user: User | null,
  forceRefresh = false, // pass true after a profile update to bypass the token cache
): Promise<CurrencyCode> {
  // No user means no preferences — fall back to the app default
  if (!user) {
    return DEFAULT_CURRENCY;
  }

  try {
    // getIdTokenResult decodes the JWT and exposes custom claims set by the backend
    const tokenResult = await user.getIdTokenResult(forceRefresh);
    const currencyClaim = tokenResult.claims[CURRENCY_CLAIM_KEY];

    // Only accept currencies that this app actually supports
    if (isSupportedCurrency(currencyClaim)) {
      return currencyClaim;
    }

    // Unknown or missing claim — silently fall back to the default currency
    return DEFAULT_CURRENCY;
  } catch (error) {
    console.warn('Failed to extract currency from token, using default:', error);
    return DEFAULT_CURRENCY;
  }
}

// Type guard — confirms the value is a string that appears in our supported currency list
function isSupportedCurrency(value: unknown): value is CurrencyCode {
  return typeof value === 'string' && SUPPORTED_CURRENCIES.includes(value as CurrencyCode);
}
