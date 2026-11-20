'use client';

/**
 * Autorregistro de cuenta con rol Cliente (Portal Cliente, roadmap §18).
 * El backend crea la cuenta en Firebase Authentication y el usuario
 * interno; aquí se recolectan los datos y, tras el alta, se inicia
 * sesión automáticamente para cargar perfil/permisos.
 */

import { useEffect, useState, type FormEvent } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/features/auth/auth-provider';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput } from '@/components/ui/form-fields';
import { ApiStatusBadge } from '@/components/ui/api-status-badge';
import { ApiError } from '@/services/api-client';
import { register } from '@/services/auth';

const MIN_PASSWORD_LENGTH = 6;

export default function RegisterPage() {
  const router = useRouter();
  const { configured, firebaseUser, profile, account, profileLoading, login, loginWithGoogle } =
    useAuth();

  const [nombre, setNombre] = useState('');
  const [apellido, setApellido] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [googleError, setGoogleError] = useState<string | null>(null);
  const [googleSubmitting, setGoogleSubmitting] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const authenticated = Boolean(firebaseUser && profile);

  function safeRedirect(): string | null {
    const raw = new URLSearchParams(window.location.search).get('redirect');
    return raw && raw.startsWith('/') && !raw.startsWith('//') ? raw : null;
  }

  // El autorregistro crea cuentas con rol Cliente: entra al portal (AGENTS.md §9).
  useEffect(() => {
    if (authenticated && !profileLoading) {
      router.replace(
        safeRedirect() ?? (account?.rol === 'Cliente' ? '/portal' : '/dashboard')
      );
    }
  }, [authenticated, profileLoading, account, router]);

  function validate(): boolean {
    if (!nombre.trim()) {
      setFormError('Ingresá tu nombre.');
      return false;
    }
    if (!apellido.trim()) {
      setFormError('Ingresá tu apellido.');
      return false;
    }
    if (!email.trim()) {
      setFormError('Ingresá tu email.');
      return false;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setFormError('El email no tiene un formato válido.');
      return false;
    }
    if (password.length < MIN_PASSWORD_LENGTH) {
      setFormError(`La contraseña debe tener al menos ${MIN_PASSWORD_LENGTH} caracteres.`);
      return false;
    }
    if (password !== confirmPassword) {
      setFormError('Las contraseñas no coinciden.');
      return false;
    }
    setFormError(null);
    return true;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!validate()) {
      return;
    }

    setSubmitting(true);
    setFormError(null);
    try {
      await register({
        email: email.trim(),
        password,
        nombre: nombre.trim(),
        apellido: apellido.trim(),
      });
    } catch (err) {
      if (err instanceof ApiError && err.isConflict) {
        setFormError('Ya existe una cuenta con ese email. Probá iniciar sesión.');
      } else if (err instanceof ApiError && err.isValidation) {
        setFormError(err.message);
      } else if (err instanceof ApiError && err.status === 429) {
        setFormError('Demasiadas solicitudes. Esperá unos minutos y volvé a intentar.');
      } else {
        setFormError('No se pudo crear la cuenta. Intentá de nuevo en unos minutos.');
      }
      setSubmitting(false);
      return;
    }

    // Alta OK: iniciar sesión para cargar perfil/permisos y redirigir.
    try {
      await login(email.trim(), password);
    } catch {
      setFormError(
        'La cuenta se creó correctamente, pero no se pudo iniciar sesión automáticamente. Volvé a la pantalla de ingreso.'
      );
      setSubmitting(false);
    }
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
        err instanceof Error ? err.message : 'No se pudo registrar con Google.'
      );
    } finally {
      setGoogleSubmitting(false);
    }
  }

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center p-3">
      <div className="w-100" style={{ maxWidth: '26rem' }}>
        <div className="text-center mb-4">
          <i className="bi bi-person-plus fs-1 text-primary" aria-hidden="true" />
          <h1 className="h4 mt-2 mb-1">Crear cuenta</h1>
          <p className="text-muted small mb-2">Sistema de Gestión · registro de cliente</p>
          <ApiStatusBadge />
        </div>

        <div className="card shadow-sm">
          <div className="card-body p-4">
            {!configured ? (
              <AppAlert variant="warning" title="Firebase sin configurar">
                Completá las claves <code>NEXT_PUBLIC_FIREBASE_*</code> en{' '}
                <code>frontend/.env.local</code> para habilitar el registro.
              </AppAlert>
            ) : null}

            {formError ? (
              <AppAlert variant="danger" title="No se pudo crear la cuenta" onClose={() => setFormError(null)}>
                {formError}
              </AppAlert>
            ) : null}

            {googleError ? (
              <AppAlert variant="danger" title="No se pudo registrar con Google" onClose={() => setGoogleError(null)}>
                {googleError}
              </AppAlert>
            ) : null}

            <form onSubmit={onSubmit} noValidate>
              <div className="row g-2">
                <div className="col-sm-6">
                  <AppInput
                    label="Nombre"
                    name="nombre"
                    autoComplete="given-name"
                    required
                    value={nombre}
                    onChange={(event) => setNombre(event.target.value)}
                  />
                </div>
                <div className="col-sm-6">
                  <AppInput
                    label="Apellido"
                    name="apellido"
                    autoComplete="family-name"
                    required
                    value={apellido}
                    onChange={(event) => setApellido(event.target.value)}
                  />
                </div>
              </div>
              <AppInput
                label="Email"
                name="email"
                type="email"
                autoComplete="email"
                placeholder="usuario@ejemplo.com"
                required
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
              <AppInput
                label="Contraseña"
                name="password"
                type="password"
                autoComplete="new-password"
                required
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
              <AppInput
                label="Repetir contraseña"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                required
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
              />
              <AppButton
                type="submit"
                className="w-100"
                loading={submitting}
                disabled={!configured}
              >
                Crear cuenta
              </AppButton>
            </form>

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
              Registrarme con Google
            </button>

            <div className="text-center mt-3">
              <Link href="/login" className="btn btn-link btn-sm p-0">
                ¿Ya tenés cuenta? Ingresá
              </Link>
            </div>
          </div>
        </div>

        <p className="text-center small text-muted mt-3 mb-0">
          ¿Problemas para registrarte? Contactá al administrador del sistema.
        </p>
      </div>
    </div>
  );
}
