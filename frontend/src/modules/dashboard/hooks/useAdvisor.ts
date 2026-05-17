import { apiFetch } from '@/api/client';
import { AdvisorResultSchema } from '@/api/schemas';
import type { AdvisorResult } from '@/api/types';
import { useState } from 'react';

export interface UseAdvisorOptions {
  claudeApiKey: string;
  onClaudeKeyRequired: () => void;
}

export interface UseAdvisorReturn {
  advisor: AdvisorResult | null;
  loading: boolean;
  error: string | null;
  runAdvisor: (provider: 'claude' | 'ollama', goals: string[]) => Promise<void>;
  clearAdvisor: () => void;
}

/**
 * Hook for managing AI advisor state and operations
 */
export function useAdvisor(options: UseAdvisorOptions): UseAdvisorReturn {
  const { claudeApiKey, onClaudeKeyRequired } = options;
  const [advisor, setAdvisor] = useState<AdvisorResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runAdvisor = async (provider: 'claude' | 'ollama', goals: string[]) => {
    // Check if Claude API key is needed
    if (provider === 'claude' && !claudeApiKey) {
      onClaudeKeyRequired();
      return;
    }

    if (goals.length === 0) {
      setError('Please select at least one goal or enter a custom question');
      return;
    }

    setLoading(true);
    setAdvisor(null);
    setError(null);

    try {
      const result = await apiFetch('/api/advisor/analyse', AdvisorResultSchema, {
        method: 'POST',
        body: JSON.stringify({
          provider,
          goals,
          apiKey: provider === 'claude' ? claudeApiKey : undefined,
        }),
      });
      setAdvisor(result);
    } catch (e) {
      console.error(e);
      setError(e instanceof Error ? e.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const clearAdvisor = () => {
    setAdvisor(null);
    setError(null);
  };

  return {
    advisor,
    loading,
    error,
    runAdvisor,
    clearAdvisor,
  };
}
