import { auth, firebaseConfigured } from '@/firebase';
import { DEFAULT_CURRENCY } from '@/utils/currency/constants';
import type { CurrencyCode } from '@/utils/currency/constants';
import { getCurrencyFromToken } from '@/utils/currency/tokenExtractor';
import { onAuthStateChanged } from 'firebase/auth';
import { createContext, useContext, useEffect, useState } from 'react';

interface CurrencyContextValue {
  currency: CurrencyCode;
  isLoading: boolean;
  refreshCurrency: () => Promise<void>;
}

// Context that shares the active user's currency preference across the component tree
const CurrencyContext = createContext<CurrencyContextValue | undefined>(undefined);

// Provides the user's preferred currency to the whole app by reading it from the Firebase token
export function CurrencyProvider({ children }: { children: React.ReactNode }) {
  const [currency, setCurrency] = useState<CurrencyCode>(DEFAULT_CURRENCY);
  // Start in a loading state only when Firebase is actually configured, so non-auth environments don't wait
  const [isLoading, setIsLoading] = useState(firebaseConfigured);

  // Triggers when the component mounts; subscribes to Firebase auth state changes so the currency
  // is always in sync with which user is currently signed in
  useEffect(() => {
    if (!firebaseConfigured || !auth) {
      // Firebase not set up — skip loading and use the default currency
      setIsLoading(false);
      return;
    }

    // onAuthStateChanged returns an unsubscribe function used for cleanup
    return onAuthStateChanged(auth, async (user) => {
      try {
        // Read the currency claim embedded in the Firebase ID token
        const currencyCode = await getCurrencyFromToken(user);
        setCurrency(currencyCode);
      } catch (error) {
        console.error('Error loading currency:', error);
        setCurrency(DEFAULT_CURRENCY);
      } finally {
        setIsLoading(false);
      }
    });
  }, []); // Empty deps — this subscription should only be set up once on mount

  // Forces a fresh token fetch so the currency reflects the latest profile change without a re-login
  const refreshCurrency = async () => {
    if (!auth?.currentUser) return;
    // forceRefresh=true bypasses the token cache to pick up newly set custom claims
    const currencyCode = await getCurrencyFromToken(auth.currentUser, true);
    setCurrency(currencyCode);
  };

  return (
    <CurrencyContext.Provider value={{ currency, isLoading, refreshCurrency }}>
      {children}
    </CurrencyContext.Provider>
  );
}

// Hook to access the current user's currency preference; throws if called outside CurrencyProvider
export function useCurrency(): CurrencyContextValue {
  const context = useContext(CurrencyContext);

  if (context === undefined) {
    throw new Error('useCurrency must be used within a CurrencyProvider');
  }

  return context;
}
