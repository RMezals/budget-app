import { auth, firebaseConfigured } from '@/firebase';
import { DEFAULT_CURRENCY } from '@/utils/currency/constants';
import type { CurrencyCode } from '@/utils/currency/constants';
import { getCurrencyFromToken } from '@/utils/currency/tokenExtractor';
import { onAuthStateChanged } from 'firebase/auth';
import { createContext, useContext, useEffect, useState } from 'react';

interface CurrencyContextValue {
  currency: CurrencyCode;
  isLoading: boolean;
}

const CurrencyContext = createContext<CurrencyContextValue | undefined>(undefined);

export function CurrencyProvider({ children }: { children: React.ReactNode }) {
  const [currency, setCurrency] = useState<CurrencyCode>(DEFAULT_CURRENCY);
  const [isLoading, setIsLoading] = useState(firebaseConfigured);

  useEffect(() => {
    if (!firebaseConfigured || !auth) {
      setIsLoading(false);
      return;
    }

    return onAuthStateChanged(auth, async (user) => {
      try {
        const currencyCode = await getCurrencyFromToken(user);
        setCurrency(currencyCode);
      } catch (error) {
        console.error('Error loading currency:', error);
        setCurrency(DEFAULT_CURRENCY);
      } finally {
        setIsLoading(false);
      }
    });
  }, []);

  return (
    <CurrencyContext.Provider value={{ currency, isLoading }}>{children}</CurrencyContext.Provider>
  );
}

/**
 * Hook to access currency context
 * @returns Current currency code and loading state
 * @throws Error if used outside CurrencyProvider
 */
export function useCurrency(): CurrencyContextValue {
  const context = useContext(CurrencyContext);

  if (context === undefined) {
    throw new Error('useCurrency must be used within a CurrencyProvider');
  }

  return context;
}
