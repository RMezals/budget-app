// Client-side encryption utility for sensitive data like API keys.
// Uses the browser's built-in Web Crypto API with AES-GCM (authenticated encryption).
// Security Note: the encryption key is stored in localStorage, so this protects against
// casual inspection but not against XSS attacks. For production consider server-side storage.

// localStorage key where the serialised encryption key is persisted between sessions
const ENCRYPTION_KEY_NAME = 'app_encryption_key';
const ALGORITHM = 'AES-GCM';
const KEY_LENGTH = 256;

// Returns the existing AES key from localStorage or generates and stores a brand new one
async function getOrCreateEncryptionKey(): Promise<CryptoKey> {
  // Try to get existing key from localStorage (stored as JSON Web Key format)
  const storedKeyData = localStorage.getItem(ENCRYPTION_KEY_NAME);

  if (storedKeyData) {
    try {
      const keyData = JSON.parse(storedKeyData);
      // importKey re-hydrates the raw JWK object into a usable CryptoKey
      const key = await crypto.subtle.importKey(
        'jwk',
        keyData,
        { name: ALGORITHM, length: KEY_LENGTH },
        true,
        ['encrypt', 'decrypt'],
      );
      return key;
    } catch (error) {
      // If the stored key is corrupted, fall through and generate a fresh one
      console.warn('Failed to import stored key, generating new one:', error);
    }
  }

  // Generate a new random AES-256 key
  const key = await crypto.subtle.generateKey(
    { name: ALGORITHM, length: KEY_LENGTH },
    true, // extractable — must be true so we can export and persist it
    ['encrypt', 'decrypt'],
  );

  // Export as JWK so it can be serialised to a plain string for localStorage
  const exportedKey = await crypto.subtle.exportKey('jwk', key);
  localStorage.setItem(ENCRYPTION_KEY_NAME, JSON.stringify(exportedKey));

  return key;
}

// Encrypts a plaintext string and returns a single base64 string containing the IV + ciphertext
export async function encryptValue(plaintext: string): Promise<string> {
  if (!plaintext) return '';

  const key = await getOrCreateEncryptionKey();
  // AES-GCM requires a unique Initialisation Vector (IV) for every encryption operation
  const iv = crypto.getRandomValues(new Uint8Array(12)); // 96-bit IV is the recommended size for GCM
  const encoder = new TextEncoder();
  const data = encoder.encode(plaintext);

  const encrypted = await crypto.subtle.encrypt({ name: ALGORITHM, iv }, key, data);

  // Prepend the IV to the ciphertext so decryption always has it available
  const combined = new Uint8Array(iv.length + encrypted.byteLength);
  combined.set(iv, 0);
  combined.set(new Uint8Array(encrypted), iv.length);

  // Convert the raw bytes to a base64 string safe for localStorage
  return btoa(String.fromCharCode(...combined));
}

// Decrypts a base64-encoded string that was previously produced by encryptValue
export async function decryptValue(encryptedData: string): Promise<string> {
  if (!encryptedData) return '';

  try {
    const key = await getOrCreateEncryptionKey();

    // Decode the base64 string back to raw bytes
    const combined = Uint8Array.from(atob(encryptedData), (c) => c.charCodeAt(0));

    // Split the combined buffer back into the 12-byte IV and the remaining ciphertext
    const iv = combined.slice(0, 12);
    const data = combined.slice(12);

    const decrypted = await crypto.subtle.decrypt({ name: ALGORITHM, iv }, key, data);

    const decoder = new TextDecoder();
    return decoder.decode(decrypted);
  } catch (error) {
    // Decryption can fail if the key has been cleared or the data is corrupted
    console.error('Decryption failed:', error);
    return '';
  }
}

// Removes the stored encryption key — all previously encrypted values become unreadable.
// Call this on logout so another user on the same device cannot access the previous user's data.
export function clearEncryptionKey(): void {
  localStorage.removeItem(ENCRYPTION_KEY_NAME);
}
