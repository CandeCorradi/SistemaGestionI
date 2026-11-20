/**
 * Inicialización del SDK web de Firebase con la configuración pública del
 * proyecto (`NEXT_PUBLIC_FIREBASE_*`). Sin credenciales la app arranca en
 * modo "sin login" y la UI muestra el aviso de configuración; no rompe el
 * build (AGENTS.md §46: secretos solo en variables de entorno).
 */

import { getApps, initializeApp, type FirebaseApp } from 'firebase/app';
import { getAuth, type Auth } from 'firebase/auth';

export interface FirebaseWebConfig {
  apiKey: string;
  authDomain: string;
  projectId: string;
  storageBucket?: string;
  messagingSenderId?: string;
  appId?: string;
}

const env = process.env;

export const firebaseConfig: FirebaseWebConfig | null = env.NEXT_PUBLIC_FIREBASE_API_KEY
  ? {
      apiKey: env.NEXT_PUBLIC_FIREBASE_API_KEY,
      authDomain: env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN ?? '',
      projectId: env.NEXT_PUBLIC_FIREBASE_PROJECT_ID ?? '',
      storageBucket: env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET,
      messagingSenderId: env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID,
      appId: env.NEXT_PUBLIC_FIREBASE_APP_ID,
    }
  : null;

/** true cuando hay configuración de Firebase en el entorno. */
export const isFirebaseConfigured = firebaseConfig !== null;

let app: FirebaseApp | null = null;
let auth: Auth | null = null;

/** App de Firebase (singleton). null si no está configurada. */
export function getFirebaseApp(): FirebaseApp | null {
  if (!firebaseConfig) {
    return null;
  }
  if (!app) {
    app = getApps()[0] ?? initializeApp(firebaseConfig);
  }
  return app;
}

/** Auth de Firebase (singleton). null si no está configurada. */
export function getFirebaseAuth(): Auth | null {
  const firebaseApp = getFirebaseApp();
  if (!firebaseApp) {
    return null;
  }
  if (!auth) {
    auth = getAuth(firebaseApp);
  }
  return auth;
}
