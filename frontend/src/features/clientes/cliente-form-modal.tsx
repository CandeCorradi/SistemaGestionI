'use client';

/**
 * Formulario de alta/edición de clientes en modal. Ejecuta sus validaciones
 * en cliente para UX, pero el backend revalida todo (AGENTS.md §31);
 * muestra los errores del API (400 validación, 409 duplicado).
 */

import { useState, type FormEvent } from 'react';
import { ApiError } from '@/services/api-client';
import { createCliente, updateCliente } from '@/services/clientes';
import type { Cliente } from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppTextarea } from '@/components/ui/form-fields';

interface ClienteFormModalProps {
  open: boolean;
  /** null = crear; con valor = editar. */
  cliente: Cliente | null;
  onClose: () => void;
  onSaved: () => void;
}

interface FormState {
  nombre: string;
  apellido: string;
  email: string;
  telefono: string;
  documento: string;
  direccion: string;
  notas: string;
}

const EMPTY: FormState = {
  nombre: '',
  apellido: '',
  email: '',
  telefono: '',
  documento: '',
  direccion: '',
  notas: '',
};

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function ClienteFormModal({ open, cliente, onClose, onSaved }: ClienteFormModalProps) {
  // El componente solo se monta con el modal abierto (render condicional
  // desde la página), por lo que el estado se inicializa con el cliente
  // actual y no requiere resets en efectos.
  const [values, setValues] = useState<FormState>(() =>
    cliente
      ? {
          nombre: cliente.nombre,
          apellido: cliente.apellido,
          email: cliente.email,
          telefono: cliente.telefono ?? '',
          documento: cliente.documento ?? '',
          direccion: cliente.direccion ?? '',
          notas: cliente.notas ?? '',
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
    if (!values.apellido.trim()) {
      next.apellido = 'El apellido es obligatorio.';
    }
    if (!values.email.trim()) {
      next.email = 'El email es obligatorio.';
    } else if (!EMAIL_PATTERN.test(values.email.trim())) {
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
        apellido: values.apellido.trim(),
        email: values.email.trim(),
        telefono: values.telefono.trim() || null,
        documento: values.documento.trim() || null,
        direccion: values.direccion.trim() || null,
      };

      if (cliente) {
        await updateCliente(cliente.id, { ...input, notas: values.notas.trim() || null });
      } else {
        await createCliente(input);
      }
      onSaved();
    } catch (err) {
      if (err instanceof ApiError) {
        setApiError(err.message);
      } else {
        setApiError('No se pudo guardar el cliente.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      title={cliente ? `Editar cliente #${cliente.id}` : 'Nuevo cliente'}
      onClose={onClose}
      size="modal-lg"
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={submitting}>
            Cancelar
          </AppButton>
          <AppButton type="submit" form="cliente-form" loading={submitting}>
            {cliente ? 'Guardar cambios' : 'Crear cliente'}
          </AppButton>
        </>
      }
    >
      {apiError ? (
        <AppAlert variant="danger" onClose={() => setApiError(null)}>
          {apiError}
        </AppAlert>
      ) : null}

      <form id="cliente-form" onSubmit={onSubmit} noValidate>
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
              label="Apellido"
              name="apellido"
              required
              value={values.apellido}
              error={errors.apellido}
              onChange={(event) => set('apellido', event.target.value)}
            />
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <AppInput
              label="Email"
              name="email"
              type="email"
              required
              value={values.email}
              error={errors.email}
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

        <div className="row">
          <div className="col-md-6">
            <AppInput
              label="Documento"
              name="documento"
              value={values.documento}
              onChange={(event) => set('documento', event.target.value)}
            />
          </div>
          <div className="col-md-6">
            <AppInput
              label="Dirección"
              name="direccion"
              value={values.direccion}
              onChange={(event) => set('direccion', event.target.value)}
            />
          </div>
        </div>

        {cliente ? (
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
