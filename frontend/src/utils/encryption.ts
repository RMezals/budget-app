/**
 * Client-side encryption utility for sensitive data like API keys
 * Uses Web Crypto API with AES-GCM encryption
 *
 * Security Note: Client-side encryption provides protection against casual inspection
 * and some attack vectors, but the encryption key must be derivable from client-side data.
 * This is more secure than plain text but not as secure as server-side encryption.
 */

const ENCRYPTION_KEY_NAME = 'app_encryption_key';
const ALGORITHM = 'AES-GCM';
const KEY_LENGTH = 256;

/**
 * Generates or retrieves a persistent encryption key
 * The key is stored in IndexedDB for persistence across sessions
 */
async function getOrCreateEncryptionKey(): Promise<CryptoKey> {
  // Try to get existing key from IndexedDB
  const storedKeyData = localStorage.getItem(ENCRYPTION_KEY_NAME);

  if (storedKeyData) {
    try {
      const keyData = JSON.parse(storedKeyData);
      const key = await crypto.subtle.importKey(
        'jwk',
        keyData,
        { name: ALGORITHM, length: KEY_LENGTH },
        true,
        ['encrypt', 'decrypt'],
      );
      return key;
    } catch (error) {
      console.warn('Failed to import stored key, generating new one:', error);
    }
  }

  // Generate new key
  const key = await crypto.subtle.generateKey(
    { name: ALGORITHM, length: KEY_LENGTH },
    true, // extractable
    ['encrypt', 'decrypt'],
  );

  // Store key for future use
  const exportedKey = await crypto.subtle.exportKey('jwk', key);
  localStorage.setItem(ENCRYPTION_KEY_NAME, JSON.stringify(exportedKey));

  return key;
}

/**
 * Encrypts a string value
 * @param plaintext - The string to encrypt
 * @returns Base64-encoded encrypted data with IV
 */
export async function encryptValue(plaintext: string): Promise<string> {
  if (!plaintext) return '';

  const key = await getOrCreateEncryptionKey();
  const iv = crypto.getRandomValues(new Uint8Array(12)); // 96-bit IV for GCM
  const encoder = new TextEncoder();
  const data = encoder.encode(plaintext);

  const encrypted = await crypto.subtle.encrypt({ name: ALGORITHM, iv }, key, data);

  // Combine IV and encrypted data
  const combined = new Uint8Array(iv.length + encrypted.byteLength);
  combined.set(iv, 0);
  combined.set(new Uint8Array(encrypted), iv.length);

  // Convert to base64
  return btoa(String.fromCharCode(...combined));
}

/**
 * Decrypts an encrypted string value
 * @param encryptedData - Base64-encoded encrypted data with IV
 * @returns Decrypted plaintext string
 */
export async function decryptValue(encryptedData: string): Promise<string> {
  if (!encryptedData) return '';

  try {
    const key = await getOrCreateEncryptionKey();

    // Decode from base64
    const combined = Uint8Array.from(atob(encryptedData), (c) => c.charCodeAt(0));

    // Extract IV and encrypted data
    const iv = combined.slice(0, 12);
    const data = combined.slice(12);

    const decrypted = await crypto.subtle.decrypt({ name: ALGORITHM, iv }, key, data);

    const decoder = new TextDecoder();
    return decoder.decode(decrypted);
  } catch (error) {
    console.error('Decryption failed:', error);
    return '';
  }
}

/**
 * Clears the encryption key, making all encrypted data unreadable
 * Use this when the user logs out or wants to reset their encryption
 */
export function clearEncryptionKey(): void {
  localStorage.removeItem(ENCRYPTION_KEY_NAME);
}
