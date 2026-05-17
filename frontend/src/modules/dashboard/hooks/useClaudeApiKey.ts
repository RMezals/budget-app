import { useEffect, useState } from 'react';
import { decryptValue, encryptValue } from '@/utils/encryption';

export interface UseClaudeApiKeyReturn {
  apiKey: string;
  showModal: boolean;
  tempKey: string;
  setTempKey: (key: string) => void;
  openModal: () => void;
  closeModal: () => void;
  saveApiKey: () => Promise<void>;
  clearApiKey: () => void;
}

/**
 * Hook for managing Claude API key storage and encryption
 *
 * SECURITY NOTE: This implementation stores the encryption key in localStorage,
 * which provides only basic obfuscation, not true security. The key is still
 * accessible to XSS attacks. For production use, consider server-side storage
 * with user authentication.
 */
export function useClaudeApiKey(): UseClaudeApiKeyReturn {
  const [apiKey, setApiKey] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [tempKey, setTempKey] = useState('');

  // Load API key on mount
  useEffect(() => {
    const loadApiKey = async () => {
      // Try to load encrypted key first
      const encryptedKey = localStorage.getItem('claudeApiKey_encrypted');
      if (encryptedKey) {
        const decryptedKey = await decryptValue(encryptedKey);
        if (decryptedKey) {
          setApiKey(decryptedKey);
          return;
        }
      }

      // Migration: Check for old plain text key
      const plainKey = localStorage.getItem('claudeApiKey');
      if (plainKey) {
        // Encrypt and migrate to new format
        const encrypted = await encryptValue(plainKey);
        localStorage.setItem('claudeApiKey_encrypted', encrypted);
        localStorage.removeItem('claudeApiKey');
        setApiKey(plainKey);
      }
    };
    loadApiKey();
  }, []);

  const openModal = () => {
    setTempKey(apiKey);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setTempKey('');
  };

  const saveApiKey = async () => {
    if (tempKey.trim()) {
      const encrypted = await encryptValue(tempKey.trim());
      localStorage.setItem('claudeApiKey_encrypted', encrypted);
      setApiKey(tempKey.trim());
    } else {
      localStorage.removeItem('claudeApiKey_encrypted');
      setApiKey('');
    }
    closeModal();
  };

  const clearApiKey = () => {
    localStorage.removeItem('claudeApiKey_encrypted');
    localStorage.removeItem('claudeApiKey'); // Also remove old format
    setApiKey('');
  };

  return {
    apiKey,
    showModal,
    tempKey,
    setTempKey,
    openModal,
    closeModal,
    saveApiKey,
    clearApiKey,
  };
}
