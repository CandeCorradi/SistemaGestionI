'use client';

/**
 * Proveedor de autenticación (roadmap §18): sesión con Firebase
 * Authentication en el cliente + perfil/permisos reales desde la API.
 *
 * - El ID Token se registra en el api-client y se refresca automáticamente
 *   por el SDK de Firebase en cada request.
 * - Los permisos efectivos vienen del backend (`/auth/permissions`) y solo
 *   se usan para ocultar UI: la seguridad real es del backend (BR-003).
 */

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import {
  GoogleAuthProvider,
  onAuthStateChanged,
  sendPasswordResetEmail,
  signInWithEmailAndPassword,
  signInWithPopup,
  signOut,
  type User,
} from 'firebase/auth';
import { getFirebaseAuth, isFirebaseConfigured } from '@/config/firebase';
import { setAuthTokenGetter, ApiError } from '@/services/api-client';
import {
  getMe,
  getMyPermissions,
  getMyProfile,
  provisionGoogleAccount,
} from '@/services/auth';
import type { AuthProfile, AuthUser } from '@/types/api';

interface AuthContextValue {
  /** false cuando faltan las variables NEXT_PUBLIC_FIREBASE_*. */
  configured: boolean;
  /** true mientras se resuelve el estado inicial de sesión. */
  initializing: boolean;
  /** Usuario de Firebase (null sin sesión). */
  firebaseUser: User | null;
  /** Identidad de la API (/auth/me). */
  profile: AuthUser | null;
  /** Perfil del usuario interno (/auth/profile). */
  account: AuthProfile | null;
  /** Permisos efectivos del backend. */
  permissions: string[];
  profileLoading: boolean;
  /** Aviso cuando el uid no tiene usuario interno habilitado. */
  accessDenied: boolean;
  error: string | null;
  can: (permission: string) => boolean;
  login: (email: string, password: string) => Promise<void>;
  /** Inicia sesión con Google (popup) y aprovisiona la cuenta interna. */
  loginWithGoogle: () => Promise<void>;
  logout: () => Promise<void>;
  /** Envía el correo de recuperación de contraseña de Firebase. */
  sendPasswordReset: (email: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Reintenta una operación ante fallos transitorios (red o 5xx). Los
 * errores 4xx son definitivos y se propagan de inmediato.
 */
async function withTransientRetry<T>(operation: () => Promise<T>, attempts = 3): Promise<T> {
  let lastError: unknown;
  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      return await operation();
    } catch (err) {
      lastError = err;
      const retriable =
        !(err instanceof ApiError) || err.status >= 500 || err.status === 0;
      if (!retriable || attempt === attempts) {
        throw err;
      }
      await new Promise((resolve) => setTimeout(resolve, 400 * attempt));
    }
  }
  throw lastError;
}

/** Mensajes de error de Firebase en español (códigos públicos del SDK). */
function mapFirebaseError(code: string, fallback?: string): string {
  switch (code) {
    case 'auth/invalid-credential':
    case 'auth/wrong-password':
    case 'auth/user-not-found':
      return 'Email o contraseña incorrectos.';
    case 'auth/too-many-requests':
      return 'Demasiados intentos. Esperá unos minutos y volvé a intentar.';
    case 'auth/invalid-email':
      return 'El email no tiene un formato válido.';
    case 'auth/network-request-failed':
      return 'Sin conexión con el servidor de autenticación.';
    case 'auth/popup-closed-by-user':
    case 'auth/cancelled-popup-request':
      return 'Cancelaste el ingreso con Google.';
    case 'auth/popup-blocked':
      return 'El navegador bloqueó la ventana de Google. Habilitá las ventanas emergentes e intentá de nuevo.';
    case 'auth/account-exists-with-different-credential':
      return 'Ya existe una cuenta con ese email usando otro método de ingreso.';
    case 'auth/operation-not-allowed':
      return 'El ingreso con Google no está habilitado en Firebase.';
    default:
      return fallback ?? 'No se pudo iniciar sesión. Revisá la configuración e intentá de nuevo.';
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [initializing, setInitializing] = useState(isFirebaseConfigured);
  const [firebaseUser, setFirebaseUser] = useState<User | null>(null);
  const [profile, setProfile] = useState<AuthUser | null>(null);
  const [account, setAccount] = useState<AuthProfile | null>(null);
  const [permissions, setPermissions] = useState<string[]>([]);
  const [profileLoading, setProfileLoading] = useState(false);
  const [accessDenied, setAccessDenied] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const userRef = useRef<User | null>(null);
  const provisionedUidsRef = useRef<Set<string>>(new Set());
  useEffect(() => {
    userRef.current = firebaseUser;
  }, [firebaseUser]);

  // El api-client pide el token vigente en cada request (se refresca solo).
  useEffect(() => {
    setAuthTokenGetter(async () => {
      const user = userRef.current;
      if (!user) {
        return null;
      }
      try {
        return await user.getIdToken();
      } catch {
        return null;
      }
    });
  }, []);

  const loadProfile = useCallback(async () => {
    setProfileLoading(true);
    setAccessDenied(false);
    setError(null);
    try {
      // Los tres requests comparten el mismo token vigente. Los reintentos
      // sólo cubren fallos transitorios (red/5xx); los 4xx son definitivos.
      const [me, perms, accountData] = await Promise.all([
        withTransientRetry(() => getMe()),
        withTransientRetry(() => getMyPermissions()),
        withTransientRetry(() => getMyProfile()),
      ]);
      setProfile(me);
      setPermissions(perms.codigos ?? []);
      setAccount(accountData);
    } catch (err) {
      setProfile(null);
      setPermissions([]);
      setAccount(null);
      if (err instanceof ApiError && err.status === 404) {
        // Token válido pero sin usuario interno habilitado.
        setAccessDenied(true);
      } else if (err instanceof ApiError && err.status === 401) {
        setError('La sesión no es válida. Cerrá sesión e ingresá de nuevo.');
      } else {
        setError('No se pudo cargar el perfil desde la API.');
      }
    } finally {
      setProfileLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!isFirebaseConfigured) {
      // `initializing` ya arranca en false sin configuración.
      return;
    }

    const auth = getFirebaseAuth();
    if (!auth) {
      return;
    }

    const unsubscribe = onAuthStateChanged(auth, async (user) => {
      // El ref debe actualizarse en el callback y no (solo) en el efecto:
      // loadProfile() arranca en este mismo tick y el getter del token lee
      // userRef.current. Si esperara al re-render, el primer request saldría
      // sin Authorization y la API respondería 401.
      userRef.current = user;
      setFirebaseUser(user);
      setInitializing(false);
      if (user) {
        // Las cuentas de Google no pasan por el autorregistro con contraseña:
        // se aprovisionan en el backend (idempotente) antes de cargar el
        // perfil para que el primer /auth/profile no responda 404.
        const esGoogle = user.providerData.some((p) => p.providerId === 'google.com');
        let provisionError: string | null = null;
        if (esGoogle && !provisionedUidsRef.current.has(user.uid)) {
          try {
            await provisionGoogleAccount();
            provisionedUidsRef.current.add(user.uid);
          } catch (err) {
            // loadProfile() corre igual: si la cuenta no se pudo aprovisionar
            // mostrará el estado real (401/404) y además el motivo acá.
            provisionError =
              err instanceof ApiError
                ? err.message
                : 'No se pudo habilitar tu cuenta en el sistema.';
          }
        }
        await loadProfile();
        if (provisionError) {
          // Después de loadProfile para no pisar el mensaje (él limpia error).
          setError(provisionError);
        }
      } else {
        setProfile(null);
        setPermissions([]);
        setAccount(null);
        setAccessDenied(false);
        setError(null);
      }
    });

    return unsubscribe;
  }, [loadProfile]);

  const login = useCallback(async (email: string, password: string) => {
    const auth = getFirebaseAuth();
    if (!auth) {
      throw new Error('Firebase no está configurado.');
    }
    try {
      await signInWithEmailAndPassword(auth, email, password);
    } catch (err) {
      const code =
        err && typeof err === 'object' && 'code' in err ? String((err as { code: unknown }).code) : '';
      throw new Error(mapFirebaseError(code));
    }
  }, []);

  const loginWithGoogle = useCallback(async () => {
    const auth = getFirebaseAuth();
    if (!auth) {
      throw new Error('Firebase no está configurado.');
    }
    const provider = new GoogleAuthProvider();
    provider.setCustomParameters({ prompt: 'select_account' });
    try {
      // El aprovisionamiento del usuario interno y la carga del perfil los
      // dispara onAuthStateChanged al completarse el popup.
      await signInWithPopup(auth, provider);
    } catch (err) {
      const code =
        err && typeof err === 'object' && 'code' in err ? String((err as { code: unknown }).code) : '';
      throw new Error(mapFirebaseError(code));
    }
  }, []);

  const logout = useCallback(async () => {
    const auth = getFirebaseAuth();
    if (auth) {
      await signOut(auth);
    }
    setProfile(null);
    setPermissions([]);
    setAccount(null);
    setAccessDenied(false);
  }, []);

  const sendPasswordReset = useCallback(async (email: string) => {
    const auth = getFirebaseAuth();
    if (!auth) {
      throw new Error('Firebase no está configurado.');
    }
    try {
      await sendPasswordResetEmail(auth, email);
    } catch (err) {
      const code =
        err && typeof err === 'object' && 'code' in err ? String((err as { code: unknown }).code) : '';
      if (code === 'auth/user-not-found') {
        throw new Error('No existe una cuenta con ese email.');
      }
      throw new Error(
        mapFirebaseError(code, 'No se pudo enviar el correo de recuperación. Intentá de nuevo.')
      );
    }
  }, []);

  const can = useCallback(
    (permission: string) => permissions.includes(permission),
    [permissions]
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      configured: isFirebaseConfigured,
      initializing,
      firebaseUser,
      profile,
      account,
      permissions,
      profileLoading,
      accessDenied,
      error,
      can,
      login,
      loginWithGoogle,
      logout,
      sendPasswordReset,
    }),
    [
      initializing,
      firebaseUser,
      profile,
      account,
      permissions,
      profileLoading,
      accessDenied,
      error,
      can,
      login,
      loginWithGoogle,
      logout,
      sendPasswordReset,
    ]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/** Acceso al contexto de autenticación. Debe usarse dentro de AuthProvider. */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe usarse dentro de <AuthProvider>.');
  }
  return context;
}
