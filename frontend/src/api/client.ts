import type { z } from 'zod';
import { auth, firebaseConfigured } from '../firebase';

// Base API URL is read from the Vite environment variable, falling back to local dev server
const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

// Retrieves the Firebase ID token for the currently signed-in user to authenticate API requests
async function getToken(): Promise<string | null> {
  // If Firebase is not configured (e.g. in tests), skip authentication entirely
  if (!firebaseConfigured || !auth) return null;
  const user = auth.currentUser;
  if (!user) throw new Error('Not authenticated');
  // getIdToken() refreshes the token automatically if it has expired
  return user.getIdToken();
}

// Central fetch wrapper used by all API calls — attaches auth token and validates responses with Zod
export async function apiFetch<T>(
  path: string,
  schemaOrInit?: z.ZodType<T> | RequestInit,
  init?: RequestInit,
): Promise<T> {
  // The second argument can be either a Zod schema or plain fetch options, so detect which it is
  const isSchema = schemaOrInit && 'parse' in schemaOrInit;
  const schema = isSchema ? (schemaOrInit as z.ZodType<T>) : undefined;
  const options = isSchema ? init : (schemaOrInit as RequestInit | undefined);

  const token = await getToken();
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      // Only attach the Authorization header when a token is available
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      // Allow callers to override or add extra headers
      ...options?.headers,
    },
  });
  if (!res.ok) {
    // Include the response body in the error message to make debugging easier
    const body = await res.text().catch(() => '');
    throw new Error(`${res.status} ${res.statusText}: ${body}`);
  }
  // 204 No Content means the server intentionally returned nothing
  if (res.status === 204) return undefined as T;

  const body = await res.text();
  // Guard against empty bodies (e.g. some DELETE endpoints)
  if (!body) return undefined as T;

  const data = JSON.parse(body);
  // If a Zod schema was provided, validate the response shape; otherwise return raw data
  return schema ? schema.parse(data) : data;
}
