'use client';

/**
 * Login con Firebase Authentication (roadmap §18). La validación crítica
 * (usuario interno + permisos) la hace el backend; aquí solo se autentica
 * y se redirige al panel.
 */

import { useEffect, useState, type FormEvent } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/features/auth/auth-provider';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput } from '@/components/ui/form-fields';
import { ApiStatusBadge } from '@/components/ui/api-status-badge';

export default function LoginPage() {
  const router = useRouter();
  const {
    configured,
    firebaseUser,
    profile,
    account,
    profileLoading,
    login,
    loginWithGoogle,
    sendPasswordReset,
    error: profileError,
  } = useAuth();

  const [mode, setMode] = useState<'login' | 'reset'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [googleSubmitting, setGoogleSubmitting] = useState(false);
  const [googleError, setGoogleError] = useState<string | null>(null);
  const [resetInfo, setResetInfo] = useState<string | null>(null);
  const [resetError, setResetError] = useState<string | null>(null);
  const [resetSending, setResetSending] = useState(false);

  const authenticated = Boolean(firebaseUser && profile);

  // Destino posterior al ingreso (p. ej. finalizar una compra desde la
  // vidriera). Sólo se aceptan rutas internas relativas para evitar open
  // redirects.
  function safeRedirect(): string | null {
    const raw = new URLSearchParams(window.location.search).get('redirect');
    return raw && raw.startsWith('/') && !raw.startsWith('//') ? raw : null;
  }

  // Según el rol: el Cliente entra al portal, el equipo al panel (AGENTS.md §9).
  useEffect(() => {
    if (authenticated && !profileLoading) {
      router.replace(
        safeRedirect() ?? (account?.rol === 'Cliente' ? '/portal' : '/dashboard')
      );
    }
  }, [authenticated, profileLoading, account, router]);

  function validate(): boolean {
    if (!email.trim()) {
      setFieldError('Ingresá tu email.');
      return false;
    }
    if (mode === 'login' && !password) {
      setFieldError(null);
      setFormError('Ingresá tu contraseña.');
      return false;
    }
    setFieldError(null);
    setFormError(null);
    return true;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!validate()) {
      return;
    }

    if (mode === 'reset') {
      setResetSending(true);
      setResetError(null);
      setResetInfo(null);
      try {
        await sendPasswordReset(email.trim());
        // Mensaje estándar: no revela si la cuenta existe.
        setResetInfo(
          'Si existe una cuenta con ese email, vas a recibir un correo para restablecer tu contraseña.'
        );
      } catch (err) {
        setResetError(
          err instanceof Error ? err.message : 'No se pudo enviar el correo de recuperación.'
        );
      } finally {
        setResetSending(false);
      }
      return;
    }

    setSubmitting(true);
    setFormError(null);
    try {
      await login(email.trim(), password);
      // El AuthProvider carga el perfil; la redirección la hace el efecto.
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No se pudo iniciar sesión.');
    } finally {
      setSubmitting(false);
    }
  }

  function switchMode(next: 'login' | 'reset') {
    setMode(next);
    setFieldError(null);
    setFormError(null);
    setResetInfo(null);
    setResetError(null);
  }

  async function onGoogle() {
    setGoogleError(null);
    setFormError(null);
    setGoogleSubmitting(true);
    try {
      await loginWithGoogle();
      // El AuthProvider aprovisiona la cuenta interna y carga el perfil;
      // la redirección la hace el efecto de sesión.
    } catch (err) {
      setGoogleError(
        err instanceof Error ? err.message : 'No se pudo ingresar con Google.'
      );
    } finally {
      setGoogleSubmitting(false);
    }
  }

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center p-3">
      <div className="w-100" style={{ maxWidth: '26rem' }}>
        <div className="text-center mb-4">
          <i className="bi bi-hammer fs-1 text-primary" aria-hidden="true" />
          <h1 className="h4 mt-2 mb-1">Sistema de Gestión</h1>
          <p className="text-muted small mb-2">Carpintería · ingreso al panel</p>
          <ApiStatusBadge />
        </div>

        <div className="card shadow-sm">
          <div className="card-body p-4">
            {!configured ? (
              <AppAlert variant="warning" title="Firebase sin configurar">
                Completá las claves <code>NEXT_PUBLIC_FIREBASE_*</code> en{' '}
                <code>frontend/.env.local</code> para habilitar el ingreso.
              </AppAlert>
            ) : null}

            {profileError ? (
              <AppAlert variant="warning" title="Aviso">
                {profileError}
              </AppAlert>
            ) : null}

            {formError ? (
              <AppAlert variant="danger" title="No se pudo iniciar sesión" onClose={() => setFormError(null)}>
                {formError}
              </AppAlert>
            ) : null}

            {resetError ? (
              <AppAlert variant="danger" title="No se pudo enviar" onClose={() => setResetError(null)}>
                {resetError}
              </AppAlert>
            ) : null}

            {resetInfo ? (
              <AppAlert variant="success" title="Correo enviado" onClose={() => setResetInfo(null)}>
                {resetInfo}
              </AppAlert>
            ) : null}

            {googleError ? (
              <AppAlert variant="danger" title="No se pudo ingresar con Google" onClose={() => setGoogleError(null)}>
                {googleError}
              </AppAlert>
            ) : null}

            <form onSubmit={onSubmit} noValidate>
              <AppInput
                label="Email"
                name="email"
                type="email"
                autoComplete="email"
                placeholder="usuario@ejemplo.com"
                required
                value={email}
                error={fieldError && !email.trim() ? fieldError : null}
                onChange={(event) => setEmail(event.target.value)}
              />
              {mode === 'login' ? (
                <AppInput
                  label="Contraseña"
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  required
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                />
              ) : null}
              <AppButton
                type="submit"
                className="w-100"
                loading={mode === 'login' ? submitting : resetSending}
                disabled={!configured}
              >
                {mode === 'login' ? 'Ingresar' : 'Enviar enlace de recuperación'}
              </AppButton>
            </form>

            {mode === 'login' ? (
              <>
                <div className="d-flex align-items-center gap-2 my-3 text-muted small">
                  <hr className="flex-grow-1 m-0" />
                  <span>o</span>
                  <hr className="flex-grow-1 m-0" />
                </div>
                <button
                  type="button"
                  className="btn btn-outline-secondary w-100"
                  onClick={() => void onGoogle()}
                  disabled={!configured || googleSubmitting}
                >
                  {googleSubmitting ? (
                    <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
                  ) : (
                    <i className="bi bi-google me-2" aria-hidden="true" />
                  )}
                  Continuar con Google
                </button>
              </>
            ) : null}

            <div className="text-center mt-3">
              {mode === 'login' ? (
                <button
                  type="button"
                  className="btn btn-link btn-sm p-0"
                  onClick={() => switchMode('reset')}
                >
                  ¿Olvidaste tu contraseña?
                </button>
              ) : (
                <button
                  type="button"
                  className="btn btn-link btn-sm p-0"
                  onClick={() => switchMode('login')}
                >
                  ← Volver a iniciar sesión
                </button>
              )}
            </div>

            {mode === 'login' ? (
              <div className="text-center mt-2">
                <Link href="/register" className="btn btn-link btn-sm p-0">
                  ¿No tenés cuenta? Creá una
                </Link>
              </div>
            ) : null}
          </div>
        </div>

        <p className="text-center small text-muted mt-3 mb-0">
          ¿Problemas para ingresar? Contactá al administrador del sistema.
        </p>
      </div>
    </div>
  );
}
