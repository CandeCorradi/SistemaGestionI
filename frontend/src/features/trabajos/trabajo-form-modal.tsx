'use client';

/**
 * Formulario de alta/edición de trabajos realizados en modal
 * (AGENTS.md §23). El nombre es obligatorio; las etiquetas se envían
 * como lista (máx. 10, se normalizan en backend). La publicación
 * gobierna la vidriera pública (BR-190) y el flag de nombre de cliente
 * la privacidad (BR-191).
 */

import { useState, type FormEvent } from 'react';
import { ApiError } from '@/services/api-client';
import { createTrabajo, updateTrabajo } from '@/services/trabajos';
import type { TrabajoRealizado } from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppTextarea } from '@/components/ui/form-fields';

interface TrabajoFormModalProps {
  open: boolean;
  /** null = crear; con valor = editar. */
  trabajo: TrabajoRealizado | null;
  onClose: () => void;
  onSaved: () => void;
}

interface FormState {
  nombre: string;
  tipoMueble: string;
  clienteNombre: string;
  publicarNombreCliente: boolean;
  fecha: string;
  descripcion: string;
  medidas: string;
  melamina: string;
  accesorios: string;
  categoria: string;
  tags: string;
  destacado: boolean;
  publicado: boolean;
}

const EMPTY: FormState = {
  nombre: '',
  tipoMueble: '',
  clienteNombre: '',
  publicarNombreCliente: false,
  fecha: '',
  descripcion: '',
  medidas: '',
  melamina: '',
  accesorios: '',
  categoria: '',
  tags: '',
  destacado: false,
  publicado: false,
};

/** Fecha ISO (yyyy-MM-dd) → DateOnly de la API; vacío = sin fecha. */
function toIsoDate(value: string): string | null {
  return /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : null;
}

export function TrabajoFormModal({ open, trabajo, onClose, onSaved }: TrabajoFormModalProps) {
  // El componente solo se monta con el modal abierto (render condicional
  // desde la página): el estado se inicializa con el trabajo actual.
  const [values, setValues] = useState<FormState>(() =>
    trabajo
      ? {
          nombre: trabajo.nombre,
          tipoMueble: trabajo.tipoMueble ?? '',
          clienteNombre: trabajo.clienteNombre ?? '',
          publicarNombreCliente: trabajo.publicarNombreCliente,
          fecha: trabajo.fecha ?? '',
          descripcion: trabajo.descripcion ?? '',
          medidas: trabajo.medidas ?? '',
          melamina: trabajo.melamina ?? '',
          accesorios: trabajo.accesorios ?? '',
          categoria: trabajo.categoria ?? '',
          tags: trabajo.tags.join(', '),
          destacado: trabajo.destacado,
          publicado: trabajo.publicado,
        }
      : EMPTY
  );
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setValues((current) => ({ ...current, [key]: value }));
    if (errors[key]) {
      setErrors((current) => ({ ...current, [key]: undefined }));
    }
  }

  function parseTags(): string[] {
    return values.tags
      .split(',')
      .map((tag) => tag.trim())
      .filter((tag) => tag.length > 0);
  }

  function validate(): boolean {
    const next: Partial<Record<keyof FormState, string>> = {};
    if (!values.nombre.trim()) {
      next.nombre = 'El nombre es obligatorio.';
    } else if (values.nombre.trim().length > 200) {
      next.nombre = 'El nombre no puede superar 200 caracteres.';
    }
    const tags = parseTags();
    if (tags.length > 10) {
      next.tags = 'No se admiten más de 10 etiquetas.';
    } else if (tags.some((tag) => tag.length > 30)) {
      next.tags = 'Cada etiqueta no puede superar 30 caracteres.';
    }
    if (values.descripcion.trim().length > 1000) {
      next.descripcion = 'La descripción no puede superar 1000 caracteres.';
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
      const base = {
        nombre: values.nombre.trim(),
        tipoMueble: values.tipoMueble.trim() || null,
        clienteNombre: values.clienteNombre.trim() || null,
        publicarNombreCliente: values.publicarNombreCliente,
        fecha: toIsoDate(values.fecha),
        descripcion: values.descripcion.trim() || null,
        medidas: values.medidas.trim() || null,
        melamina: values.melamina.trim() || null,
        accesorios: values.accesorios.trim() || null,
        categoria: values.categoria.trim() || null,
        tags: parseTags(),
        destacado: values.destacado,
        publicado: values.publicado,
      };

      if (trabajo) {
        await updateTrabajo(trabajo.id, {
          ...base,
          limpiarFecha: base.fecha === null,
        });
      } else {
        await createTrabajo(base);
      }
      onSaved();
    } catch (err) {
      if (err instanceof ApiError) {
        setApiError(err.message);
      } else {
        setApiError('No se pudo guardar el trabajo.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      title={trabajo ? `Editar trabajo #${trabajo.id}` : 'Nuevo trabajo realizado'}
      onClose={onClose}
      size="modal-xl"
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={submitting}>
            Cancelar
          </AppButton>
          <AppButton type="submit" form="trabajo-form" loading={submitting}>
            {trabajo ? 'Guardar cambios' : 'Crear trabajo'}
          </AppButton>
        </>
      }
    >
      {apiError ? (
        <AppAlert variant="danger" onClose={() => setApiError(null)}>
          {apiError}
        </AppAlert>
      ) : null}

      <form id="trabajo-form" onSubmit={onSubmit} noValidate>
        <div className="row">
          <div className="col-md-6">
            <AppInput
              label="Nombre"
              name="nombre"
              required
              value={values.nombre}
              error={errors.nombre}
              hint="Título de la obra en la vidriera."
              onChange={(event) => set('nombre', event.target.value)}
            />
          </div>
          <div className="col-md-3">
            <AppInput
              label="Tipo de mueble"
              name="tipoMueble"
              placeholder="Cocina, Ropero..."
              value={values.tipoMueble}
              onChange={(event) => set('tipoMueble', event.target.value)}
            />
          </div>
          <div className="col-md-3">
            <AppInput
              label="Categoría"
              name="categoria"
              value={values.categoria}
              onChange={(event) => set('categoria', event.target.value)}
            />
          </div>
        </div>

        <div className="row">
          <div className="col-md-4">
            <AppInput
              label="Cliente"
              name="clienteNombre"
              value={values.clienteNombre}
              hint="Sólo se publica si activás la opción de abajo (BR-191)."
              onChange={(event) => set('clienteNombre', event.target.value)}
            />
          </div>
          <div className="col-md-4">
            <AppInput
              label="Fecha de realización"
              name="fecha"
              type="date"
              value={values.fecha}
              onChange={(event) => set('fecha', event.target.value)}
            />
          </div>
          <div className="col-md-4">
            <AppInput
              label="Etiquetas"
              name="tags"
              placeholder="cocina, isla, a-medida"
              value={values.tags}
              error={errors.tags}
              hint="Separadas por coma (máx. 10)."
              onChange={(event) => set('tags', event.target.value)}
            />
          </div>
        </div>

        <div className="row">
          <div className="col-md-4">
            <AppInput
              label="Medidas"
              name="medidas"
              placeholder="2400x2200x600"
              value={values.medidas}
              onChange={(event) => set('medidas', event.target.value)}
            />
          </div>
          <div className="col-md-4">
            <AppInput
              label="Melamina"
              name="melamina"
              value={values.melamina}
              onChange={(event) => set('melamina', event.target.value)}
            />
          </div>
          <div className="col-md-4">
            <AppInput
              label="Accesorios"
              name="accesorios"
              value={values.accesorios}
              onChange={(event) => set('accesorios', event.target.value)}
            />
          </div>
        </div>

        <AppTextarea
          label="Descripción"
          name="descripcion"
          rows={2}
          value={values.descripcion}
          error={errors.descripcion}
          onChange={(event) => set('descripcion', event.target.value)}
        />

        <div className="form-check mb-2">
          <input
            id="trabajo-publicar-cliente"
            className="form-check-input"
            type="checkbox"
            checked={values.publicarNombreCliente}
            onChange={(event) => set('publicarNombreCliente', event.target.checked)}
          />
          <label className="form-check-label" htmlFor="trabajo-publicar-cliente">
            Publicar el nombre del cliente en la vidriera
          </label>
          <div className="form-text">
            Si está desactivado, el nombre queda sólo para uso interno (privacidad, BR-191).
          </div>
        </div>

        <div className="form-check mb-2">
          <input
            id="trabajo-destacado"
            className="form-check-input"
            type="checkbox"
            checked={values.destacado}
            onChange={(event) => set('destacado', event.target.checked)}
          />
          <label className="form-check-label" htmlFor="trabajo-destacado">
            Destacado (se muestra primero en la vidriera)
          </label>
        </div>

        <div className="form-check mb-2">
          <input
            id="trabajo-publicado"
            className="form-check-input"
            type="checkbox"
            checked={values.publicado}
            onChange={(event) => set('publicado', event.target.checked)}
          />
          <label className="form-check-label" htmlFor="trabajo-publicado">
            Publicado en la vidriera
          </label>
          <div className="form-text">
            Sólo las obras publicadas se ven en /showroom sin sesión (BR-190).
          </div>
        </div>
      </form>
    </Modal>
  );
}
