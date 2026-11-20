'use client';

/**
 * Formulario de alta/edición de proveedores en modal (BR-020: término
 * "Proveedor"). El email es opcional en proveedores; si viene duplicado la
 * API responde 409 y se muestra aquí.
 */

import { useState, type FormEvent } from 'react';
import { ApiError } from '@/services/api-client';
import { createProveedor, updateProveedor } from '@/services/proveedores';
import type { Proveedor } from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppTextarea } from '@/components/ui/form-fields';

interface ProveedorFormModalProps {
  open: boolean;
  /** null = crear; con valor = editar. */
  proveedor: Proveedor | null;
  onClose: () => void;
  onSaved: () => void;
}

interface FormState {
  nombre: string;
  contacto: string;
  email: string;
  telefono: string;
  direccion: string;
  notas: string;
}

const EMPTY: FormState = {
  nombre: '',
  contacto: '',
  email: '',
  telefono: '',
  direccion: '',
  notas: '',
};

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function ProveedorFormModal({ open, proveedor, onClose, onSaved }: ProveedorFormModalProps) {
  // El componente solo se monta con el modal abierto (render condicional
  // desde la página): el estado se inicializa con el proveedor actual.
  const [values, setValues] = useState<FormState>(() =>
    proveedor
      ? {
          nombre: proveedor.nombre,
          contacto: proveedor.contacto ?? '',
          email: proveedor.email ?? '',
          telefono: proveedor.telefono ?? '',
          direccion: proveedor.direccion ?? '',
          notas: proveedor.notas ?? '',
        }
      : EMPTY
  );
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function set<K extends keyof FormState>(key: K, value: string) {
    setValues((current) => ({ ...current, [key]: value }));
    if (errors[key]) {
      setErrors((current) => ({ ...current, [key]: undefined }));
    }
  }

  function validate(): boolean {
    const next: Partial<Record<keyof FormState, string>> = {};
    if (!values.nombre.trim()) {
      next.nombre = 'El nombre es obligatorio.';
    }
    const email = values.email.trim();
    if (email && !EMAIL_PATTERN.test(email)) {
      next.email = 'El email no tiene un formato válido.';
    }
    setErrors(next);
    return Object.keys(next).length === 0;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!validate()) {
      return;
    }

    setSubmitting(true);
    setApiError(null);
    try {
      const input = {
        nombre: values.nombre.trim(),
        contacto: values.contacto.trim() || null,
        email: values.email.trim() || null,
        telefono: values.telefono.trim() || null,
        direccion: values.direccion.trim() || null,
      };

      if (proveedor) {
        await updateProveedor(proveedor.id, { ...input, notas: values.notas.trim() || null });
      } else {
        await createProveedor(input);
      }
      onSaved();
    } catch (err) {
      if (err instanceof ApiError) {
        setApiError(err.message);
      } else {
        setApiError('No se pudo guardar el proveedor.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      title={proveedor ? `Editar proveedor #${proveedor.id}` : 'Nuevo proveedor'}
      onClose={onClose}
      size="modal-lg"
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={submitting}>
            Cancelar
          </AppButton>
          <AppButton type="submit" form="proveedor-form" loading={submitting}>
            {proveedor ? 'Guardar cambios' : 'Crear proveedor'}
          </AppButton>
        </>
      }
    >
      {apiError ? (
        <AppAlert variant="danger" onClose={() => setApiError(null)}>
          {apiError}
        </AppAlert>
      ) : null}

      <form id="proveedor-form" onSubmit={onSubmit} noValidate>
        <div className="row">
          <div className="col-md-6">
            <AppInput
              label="Nombre"
              name="nombre"
              required
              value={values.nombre}
              error={errors.nombre}
              onChange={(event) => set('nombre', event.target.value)}
            />
          </div>
          <div className="col-md-6">
            <AppInput
              label="Persona de contacto"
              name="contacto"
              value={values.contacto}
              onChange={(event) => set('contacto', event.target.value)}
            />
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <AppInput
              label="Email"
              name="email"
              type="email"
              value={values.email}
              error={errors.email}
              hint="Opcional; si se informa, debe ser único."
              onChange={(event) => set('email', event.target.value)}
            />
          </div>
          <div className="col-md-6">
            <AppInput
              label="Teléfono"
              name="telefono"
              type="tel"
              value={values.telefono}
              onChange={(event) => set('telefono', event.target.value)}
            />
          </div>
        </div>

        <AppInput
          label="Dirección"
          name="direccion"
          value={values.direccion}
          onChange={(event) => set('direccion', event.target.value)}
        />

        {proveedor ? (
          <AppTextarea
            label="Notas"
            name="notas"
            rows={3}
            value={values.notas}
            onChange={(event) => set('notas', event.target.value)}
          />
        ) : null}
      </form>
    </Modal>
  );
}
