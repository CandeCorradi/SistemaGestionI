'use client';

/**
 * Formulario de alta/edición de productos en modal (functional-spec §8).
 * El SKU es identidad única (BR-030: duplicado → 409 de la API). El stock
 * actual solo se informa al alta: luego se gestiona con inventario.
 */

import { useState, type FormEvent } from 'react';
import { ApiError } from '@/services/api-client';
import { createProducto, updateProducto } from '@/services/productos';
import { TIPO_PRODUCTO_LABELS, type Producto, type TipoProducto } from '@/types/api';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppSelect, AppTextarea } from '@/components/ui/form-fields';

interface ProductoFormModalProps {
  open: boolean;
  /** null = crear; con valor = editar. */
  producto: Producto | null;
  onClose: () => void;
  onSaved: () => void;
}

interface FormState {
  nombre: string;
  codigoSku: string;
  tipo: string;
  precio: string;
  costo: string;
  categoria: string;
  descripcion: string;
  material: string;
  acabado: string;
  color: string;
  dimensiones: string;
  stockActual: string;
  stockMinimo: string;
  showroom: boolean;
  esPersonalizado: boolean;
}

const EMPTY: FormState = {
  nombre: '',
  codigoSku: '',
  tipo: '0',
  precio: '',
  costo: '',
  categoria: '',
  descripcion: '',
  material: '',
  acabado: '',
  color: '',
  dimensiones: '',
  stockActual: '0',
  stockMinimo: '0',
  showroom: false,
  esPersonalizado: false,
};

const TIPO_OPTIONS = (Object.keys(TIPO_PRODUCTO_LABELS) as unknown as TipoProducto[]).map(
  (value) => ({
    value: String(value),
    label: TIPO_PRODUCTO_LABELS[value],
  })
);

export function ProductoFormModal({ open, producto, onClose, onSaved }: ProductoFormModalProps) {
  // El componente solo se monta con el modal abierto (render condicional
  // desde la página): el estado se inicializa con el producto actual.
  const [values, setValues] = useState<FormState>(() =>
    producto
      ? {
          nombre: producto.nombre,
          codigoSku: producto.codigoSku,
          tipo: String(producto.tipo),
          precio: String(producto.precio),
          costo: producto.costo != null ? String(producto.costo) : '',
          categoria: producto.categoria ?? '',
          descripcion: producto.descripcion ?? '',
          material: producto.material ?? '',
          acabado: producto.acabado ?? '',
          color: producto.color ?? '',
          dimensiones: producto.dimensiones ?? '',
          stockActual: String(producto.stockActual),
          stockMinimo: String(producto.stockMinimo),
          showroom: producto.showroom,
          esPersonalizado: producto.esPersonalizado,
        }
      : EMPTY
  );
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const isEdit = producto !== null;

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
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
    if (!values.codigoSku.trim()) {
      next.codigoSku = 'El código SKU es obligatorio.';
    }
    const precio = Number(values.precio);
    if (!values.precio.trim() || Number.isNaN(precio)) {
      next.precio = 'Ingresá un precio válido.';
    } else if (precio < 0) {
      next.precio = 'El precio no puede ser negativo.';
    }
    const costo = Number(values.costo);
    if (values.costo.trim() && (Number.isNaN(costo) || costo < 0)) {
      next.costo = 'El costo no puede ser negativo.';
    }
    const stockMinimo = Number(values.stockMinimo);
    if (Number.isNaN(stockMinimo) || stockMinimo < 0) {
      next.stockMinimo = 'El stock mínimo no puede ser negativo.';
    }
    if (!isEdit) {
      const stockActual = Number(values.stockActual);
      if (Number.isNaN(stockActual) || stockActual < 0) {
        next.stockActual = 'El stock no puede ser negativo.';
      }
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
        codigoSku: values.codigoSku.trim(),
        tipo: Number(values.tipo) as TipoProducto,
        precio: Number(values.precio),
        costo: values.costo.trim() ? Number(values.costo) : null,
        categoria: values.categoria.trim() || null,
        descripcion: values.descripcion.trim() || null,
        material: values.material.trim() || null,
        acabado: values.acabado.trim() || null,
        color: values.color.trim() || null,
        dimensiones: values.dimensiones.trim() || null,
        stockMinimo: Number(values.stockMinimo),
        showroom: values.showroom,
        esPersonalizado: values.esPersonalizado,
      };

      if (producto) {
        await updateProducto(producto.id, base);
      } else {
        await createProducto({ ...base, stockActual: Number(values.stockActual) });
      }
      onSaved();
    } catch (err) {
      if (err instanceof ApiError) {
        setApiError(err.message);
      } else {
        setApiError('No se pudo guardar el producto.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      title={producto ? `Editar producto #${producto.id}` : 'Nuevo producto'}
      onClose={onClose}
      size="modal-xl"
      footer={
        <>
          <AppButton variant="secondary" onClick={onClose} disabled={submitting}>
            Cancelar
          </AppButton>
          <AppButton type="submit" form="producto-form" loading={submitting}>
            {producto ? 'Guardar cambios' : 'Crear producto'}
          </AppButton>
        </>
      }
    >
      {apiError ? (
        <AppAlert variant="danger" onClose={() => setApiError(null)}>
          {apiError}
        </AppAlert>
      ) : null}

      <form id="producto-form" onSubmit={onSubmit} noValidate>
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
          <div className="col-md-3">
            <AppInput
              label="Código SKU"
              name="codigoSku"
              required
              value={values.codigoSku}
              error={errors.codigoSku}
              hint="Identidad única (BR-030)."
              onChange={(event) => set('codigoSku', event.target.value)}
            />
          </div>
          <div className="col-md-3">
            <AppSelect
              label="Tipo"
              name="tipo"
              required
              options={TIPO_OPTIONS}
              value={values.tipo}
              onChange={(event) => set('tipo', event.target.value)}
            />
          </div>
        </div>

        <div className="row">
          <div className="col-md-3">
            <AppInput
              label="Precio de venta"
              name="precio"
              type="number"
              min="0"
              step="0.01"
              required
              value={values.precio}
              error={errors.precio}
              onChange={(event) => set('precio', event.target.value)}
            />
          </div>
          <div className="col-md-3">
            <AppInput
              label="Costo"
              name="costo"
              type="number"
              min="0"
              step="0.01"
              value={values.costo}
              error={errors.costo}
              hint="Opcional; no se muestra en listados."
              onChange={(event) => set('costo', event.target.value)}
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
          <div className="col-md-3">
            <AppInput
              label="Stock mínimo"
              name="stockMinimo"
              type="number"
              min="0"
              step="1"
              value={values.stockMinimo}
              error={errors.stockMinimo}
              onChange={(event) => set('stockMinimo', event.target.value)}
            />
          </div>
        </div>

        <div className="row">
          <div className="col-md-3">
            <AppInput
              label="Material"
              name="material"
              value={values.material}
              onChange={(event) => set('material', event.target.value)}
            />
          </div>
          <div className="col-md-3">
            <AppInput
              label="Acabado"
              name="acabado"
              value={values.acabado}
              onChange={(event) => set('acabado', event.target.value)}
            />
          </div>
          <div className="col-md-3">
            <AppInput
              label="Color"
              name="color"
              value={values.color}
              onChange={(event) => set('color', event.target.value)}
            />
          </div>
          <div className="col-md-3">
            <AppInput
              label="Dimensiones"
              name="dimensiones"
              placeholder="120 x 60 x 75 cm"
              value={values.dimensiones}
              onChange={(event) => set('dimensiones', event.target.value)}
            />
          </div>
        </div>

        {!isEdit ? (
          <div className="row">
            <div className="col-md-3">
              <AppInput
                label="Stock inicial"
                name="stockActual"
                type="number"
                min="0"
                step="1"
                value={values.stockActual}
                error={errors.stockActual}
                hint="Luego se gestiona con inventario."
                onChange={(event) => set('stockActual', event.target.value)}
              />
            </div>
          </div>
        ) : null}

        <AppTextarea
          label="Descripción"
          name="descripcion"
          rows={2}
          value={values.descripcion}
          onChange={(event) => set('descripcion', event.target.value)}
        />

        <div className="form-check mb-2">
          <input
            id="producto-showroom"
            className="form-check-input"
            type="checkbox"
            checked={values.showroom}
            onChange={(event) => set('showroom', event.target.checked)}
          />
          <label className="form-check-label" htmlFor="producto-showroom">
            Visible en el showroom / vidriera
          </label>
        </div>

        <div className="form-check mb-2">
          <input
            id="producto-personalizado"
            className="form-check-input"
            type="checkbox"
            checked={values.esPersonalizado}
            onChange={(event) => set('esPersonalizado', event.target.checked)}
          />
          <label className="form-check-label" htmlFor="producto-personalizado">
            Personalizado / a medida
          </label>
          <div className="form-text">
            El catálogo distingue estándar de a medida (functional-spec §9). Las especificaciones
            de una pieza hecha para un presupuesto se conservan en el presupuesto (BR-041).
          </div>
        </div>
      </form>
    </Modal>
  );
}
