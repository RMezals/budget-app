import type { z } from 'zod';
import { auth, firebaseConfigured } from '../firebase';

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

async function getToken(): Promise<string | null> {
  if (!firebaseConfigured || !auth) return null;
  const user = auth.currentUser;
  if (!user) throw new Error('Not authenticated');
  return user.getIdToken();
}

export async function apiFetch<T>(
  path: string,
  schemaOrInit?: z.ZodType<T> | RequestInit,
  init?: RequestInit,
): Promise<T> {
  // Determine if second parameter is schema or init
  const isSchema = schemaOrInit && 'parse' in schemaOrInit;
  const schema = isSchema ? (schemaOrInit as z.ZodType<T>) : undefined;
  const options = isSchema ? init : (schemaOrInit as RequestInit | undefined);

  const token = await getToken();
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`${res.status} ${res.statusText}: ${body}`);
  }
  if (res.status === 204) return undefined as T;

  const body = await res.text();
  if (!body) return undefined as T;

  const data = JSON.parse(body);
  return schema ? schema.parse(data) : data;
}
