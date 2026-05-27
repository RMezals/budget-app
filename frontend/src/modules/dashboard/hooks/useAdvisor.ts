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

// Manages AI advisor requests, including loading state and error handling for both Claude and Ollama
export function useAdvisor(options: UseAdvisorOptions): UseAdvisorReturn {
  const { claudeApiKey, onClaudeKeyRequired } = options;
  // advisor holds the last successful response from the backend
  const [advisor, setAdvisor] = useState<AdvisorResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sends the selected goals to the backend and stores the AI-generated tips in state
  const runAdvisor = async (provider: 'claude' | 'ollama', goals: string[]) => {
    // Guard: Claude requires an API key — prompt the user to enter one before proceeding
    if (provider === 'claude' && !claudeApiKey) {
      onClaudeKeyRequired();
      return;
    }

    if (goals.length === 0) {
      setError('Please select at least one goal or enter a custom question');
      return;
    }

    // Clear any previous result and error before starting a new request
    setLoading(true);
    setAdvisor(null);
    setError(null);

    try {
      const result = await apiFetch('/api/advisor/analyse', AdvisorResultSchema, {
        method: 'POST',
        body: JSON.stringify({
          provider,
          goals,
          // Only send the API key when using Claude; Ollama runs locally and needs no key
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

  // Resets advisor and error state so the UI can return to its initial empty form
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
