'use client';

/**
 * Perfil del Portal Cliente (functional-spec §23): consulta y edición de
 * los datos de contacto del cliente vinculado. El email no es editable
 * desde el portal: es la identidad de la cuenta (BR-010).
 */

import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '@/services/api-client';
import { getProfile, updateProfile } from '@/services/portal';
import { formatDate } from '@/lib/format';
import type { ClienteProfile } from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput } from '@/components/ui/form-fields';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface FormState {
  nombre: string;
  apellido: string;
  telefono: string;
  direccion: string;
  documento: string;
}

export default function PortalProfilePage() {
  const [profile, setProfile] = useState<ClienteProfile | null>(null);
  const [form, setForm] = useState<FormState>({
    nombre: '',
    apellido: '',
    telefono: '',
    direccion: '',
    documento: '',
  });
  const [loadError, setLoadError] = useState<string | null>(null);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const result = await getProfile(controller.signal);
        if (cancelled) {
          return;
        }
        setProfile(result);
        setForm({
          nombre: result.nombre,
          apellido: result.apellido,
          telefono: result.telefono ?? '',
          direccion: result.direccion ?? '',
          documento: result.documento ?? '',
        });
        setLoadError(null);
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setLoadError(
            err instanceof ApiError && err.isNotFound
              ? 'No hay un cliente vinculado a tu cuenta.'
              : err instanceof Error
                ? err.message
                : 'No se pudo cargar el perfil.'
          );
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, []);

  function updateField(field: keyof FormState, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
    setSaved(false);
    setFieldError(null);
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setFieldError(null);
    setFormError(null);
    setSaved(false);

    if (!form.nombre.trim()) {
      setFieldError('El nombre es obligatorio.');
      return;
    }
    if (!form.apellido.trim()) {
      setFieldError('El apellido es obligatorio.');
      return;
    }

    setSaving(true);
    try {
      const updated = await updateProfile({
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        telefono: form.telefono.trim() || null,
        direccion: form.direccion.trim() || null,
        documento: form.documento.trim() || null,
      });
      setProfile(updated);
      setSaved(true);
    } catch (err) {
      setFormError(
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : 'No se pudo guardar el perfil.'
      );
    } finally {
      setSaving(false);
    }
  }

  if (loadError && !profile) {
    return <AppAlert variant="danger" title="No se pudo cargar">{loadError}</AppAlert>;
  }

  if (!profile) {
    return <LoadingSpinner label="Cargando perfil..." />;
  }

  return (
    <>
      <div className="mb-4">
        <h1 className="h4 mb-1">Mi perfil</h1>
        <p className="text-muted mb-0 small">
          Cliente #{profile.id} · alta el {formatDate(profile.fechaCreacion)}
        </p>
      </div>

      <div className="row g-3">
        <div className="col-12 col-lg-8">
          <PanelCard title="Datos de contacto">
            <div className="p-3">
              {formError ? (
                <AppAlert variant="danger" title="No se pudo guardar">
                  {formError}
                </AppAlert>
              ) : null}
              {saved ? (
                <AppAlert variant="success" title="Perfil actualizado">
                  Se guardaron tus datos de contacto.
                </AppAlert>
              ) : null}
              {fieldError ? <AppAlert variant="warning">{fieldError}</AppAlert> : null}

              <form onSubmit={(event) => void onSubmit(event)}>
                <div className="row g-3">
                  <div className="col-12 col-md-6">
                    <AppInput
                      label="Nombre"
                      name="nombre"
                      required
                      value={form.nombre}
                      onChange={(event) => updateField('nombre', event.target.value)}
                    />
                  </div>
                  <div className="col-12 col-md-6">
                    <AppInput
                      label="Apellido"
                      name="apellido"
                      required
                      value={form.apellido}
                      onChange={(event) => updateField('apellido', event.target.value)}
                    />
                  </div>
                  <div className="col-12 col-md-6">
                    <AppInput
                      label="Teléfono"
                      name="telefono"
                      type="tel"
                      value={form.telefono}
                      onChange={(event) => updateField('telefono', event.target.value)}
                    />
                  </div>
                  <div className="col-12 col-md-6">
                    <AppInput
                      label="Documento"
                      name="documento"
                      value={form.documento}
                      onChange={(event) => updateField('documento', event.target.value)}
                    />
                  </div>
                  <div className="col-12">
                    <AppInput
                      label="Dirección"
                      name="direccion"
                      value={form.direccion}
                      onChange={(event) => updateField('direccion', event.target.value)}
                    />
                  </div>
                </div>

                <div className="mt-3 d-flex justify-content-end">
                  <AppButton type="submit" loading={saving}>
                    Guardar cambios
                  </AppButton>
                </div>
              </form>
            </div>
          </PanelCard>
        </div>

        <div className="col-12 col-lg-4">
          <PanelCard title="Cuenta">
            <div className="p-3 small">
              <div className="text-uppercase text-muted fw-semibold">Email</div>
              <div className="mb-2">{profile.email}</div>
              <p className="text-muted mb-0">
                El email identifica tu cuenta de acceso y no se modifica desde el portal. Si
                necesitás cambiarlo, contactanos.
              </p>
            </div>
          </PanelCard>
        </div>
      </div>
    </>
  );
}
