import { decryptValue, encryptValue } from '@/utils/encryption';
import { useEffect, useState } from 'react';

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

// Manages storing and retrieving the user's Claude API key in localStorage using AES-GCM encryption.
// SECURITY NOTE: The encryption key is also stored in localStorage, so this is not tamper-proof
// against XSS. For a production app, consider storing the key server-side.
export function useClaudeApiKey(): UseClaudeApiKeyReturn {
  // apiKey holds the decrypted key ready for use in API calls
  const [apiKey, setApiKey] = useState('');
  // showModal controls visibility of the input dialog
  const [showModal, setShowModal] = useState(false);
  // tempKey is the in-progress value typed into the modal before saving
  const [tempKey, setTempKey] = useState('');

  // Runs once on mount to load any previously saved API key from localStorage
  useEffect(() => {
    const loadApiKey = async () => {
      // Prefer the newer encrypted storage format
      const encryptedKey = localStorage.getItem('claudeApiKey_encrypted');
      if (encryptedKey) {
        const decryptedKey = await decryptValue(encryptedKey);
        if (decryptedKey) {
          setApiKey(decryptedKey);
          return;
        }
      }

      // Migration path: older versions stored the key in plain text; encrypt it on first load
      const plainKey = localStorage.getItem('claudeApiKey');
      if (plainKey) {
        // Silently upgrade plain text key to encrypted format
        const encrypted = await encryptValue(plainKey);
        localStorage.setItem('claudeApiKey_encrypted', encrypted);
        localStorage.removeItem('claudeApiKey');
        setApiKey(plainKey);
      }
    };
    loadApiKey();
  }, []); // Empty deps — only needs to run once when the hook is first used

  // Pre-fills the modal input with the existing key so the user can review it before saving
  const openModal = () => {
    setTempKey(apiKey);
    setShowModal(true);
  };

  // Dismisses the modal and discards any unsaved changes to tempKey
  const closeModal = () => {
    setShowModal(false);
    setTempKey('');
  };

  // Encrypts and persists the key typed into the modal, or removes it if the field is blank
  const saveApiKey = async () => {
    if (tempKey.trim()) {
      const encrypted = await encryptValue(tempKey.trim());
      localStorage.setItem('claudeApiKey_encrypted', encrypted);
      setApiKey(tempKey.trim());
    } else {
      // Empty input means the user wants to remove their key
      localStorage.removeItem('claudeApiKey_encrypted');
      setApiKey('');
    }
    closeModal();
  };

  // Removes the API key from both localStorage slots and clears it from state
  const clearApiKey = () => {
    localStorage.removeItem('claudeApiKey_encrypted');
    localStorage.removeItem('claudeApiKey'); // Also remove legacy plain-text format if present
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
