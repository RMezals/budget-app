import { initializeApp } from 'firebase/app'
import { getAuth } from 'firebase/auth'

const firebaseConfig = {
  apiKey:            import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain:        import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId:         import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket:     import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId:             import.meta.env.VITE_FIREBASE_APP_ID,
}

export const firebaseConfigured = !!import.meta.env.VITE_FIREBASE_API_KEY

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const auth: ReturnType<typeof getAuth> = firebaseConfigured
  ? getAuth(initializeApp(firebaseConfig))
  : null as any
