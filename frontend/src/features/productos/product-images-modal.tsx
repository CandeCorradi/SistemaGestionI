'use client';

/**
 * Galería de imágenes de un producto (api-spec §13, AGENTS.md §24):
 * vista previa, subida al storage (JPG/PNG/WEBP/GIF validados en
 * backend, ≤ 5 MB) con optimización automática en cliente a ≤ 900 KB
 * (AGENTS.md §63) y baja con confirmación. Los binarios viven en
 * Supabase Storage; la API sólo guarda la referencia (BR-100/BR-101).
 */

import { useEffect, useState, type ChangeEvent } from 'react';
import { ApiError } from '@/services/api-client';
import { eliminarImagen, listImagenes, subirImagen } from '@/services/productos';
import type { ProductoImagen, ProductoList } from '@/types/api';
import { compressImageForUpload } from '@/lib/image-compress';
import { Modal } from '@/components/ui/modal';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

interface ProductImagesModalProps {
  open: boolean;
  producto: ProductoList;
  /** Oculta la subida/baja cuando el usuario sólo tiene PRODUCTOS_VIEW. */
  canUpload: boolean;
  onClose: () => void;
  /** Notifica cambios para recargar el listado (portada del catálogo). */
  onChanged?: () => void;
}

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
const MAX_BYTES = 5 * 1024 * 1024;

export function ProductImagesModal({
  open,
  producto,
  canUpload,
  onClose,
  onChanged,
}: ProductImagesModalProps) {
  const [imagenes, setImagenes] = useState<ProductoImagen[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<ProductoImagen | null>(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (!open) {
      return;
    }
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const result = await listImagenes(producto.id, controller.signal);
        if (!cancelled) {
          setImagenes(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(
            err instanceof ApiError && err.isNotFound
              ? 'El producto ya no existe.'
              : err instanceof Error
                ? err.message
                : 'No se pudieron cargar las imágenes.'
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [open, producto.id]);

  async function handleUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    setUploadError(null);
    if (!ALLOWED_TYPES.includes(file.type)) {
      setUploadError('Formato no permitido. Usá JPG, PNG, WEBP o GIF.');
      return;
    }
    if (file.size > MAX_BYTES) {
      setUploadError('La imagen no puede superar 5 MB.');
      return;
    }

    setUploading(true);
    try {
      // Se optimiza en cliente (redimensión + compresión) para que las
      // miniaturas carguen rápido en el catálogo y en el modal.
      const optimizada = await compressImageForUpload(file);
      const creada = await subirImagen(producto.id, optimizada);
      setImagenes((prev) => [...prev, creada].sort((a, b) => a.orden - b.orden));
      onChanged?.();
    } catch (err) {
      setUploadError(
        err instanceof ApiError && err.status === 503
          ? 'El almacenamiento de imágenes no está disponible.'
          : err instanceof Error
            ? err.message
            : 'No se pudo subir la imagen.'
      );
    } finally {
      setUploading(false);
    }
  }

  async function confirmDelete() {
    if (!confirmTarget) {
      return;
    }
    setDeleting(true);
    try {
      await eliminarImagen(producto.id, confirmTarget.id);
      setImagenes((prev) => prev.filter((img) => img.id !== confirmTarget.id));
      setConfirmTarget(null);
      onChanged?.();
    } catch (err) {
      setUploadError(
        err instanceof Error ? err.message : 'No se pudo eliminar la imagen.'
      );
      setConfirmTarget(null);
    } finally {
      setDeleting(false);
    }
  }

  return (
    <>
      <Modal
        open={open}
        title={`Imágenes · ${producto.nombre}`}
        onClose={onClose}
        size="modal-lg"
        footer={
          <AppButton variant="secondary" onClick={onClose}>
            Cerrar
          </AppButton>
        }
      >
        {uploadError ? (
          <AppAlert variant="danger" onClose={() => setUploadError(null)}>
            {uploadError}
          </AppAlert>
        ) : null}

        {canUpload ? (
          <div className="mb-3">
            <label className="form-label small text-muted mb-1" htmlFor="product-image-input">
              Agregar imagen (JPG, PNG, WEBP o GIF · hasta 5 MB · se optimiza a ≤ 900 KB)
            </label>
            <input
              id="product-image-input"
              className="form-control"
              type="file"
              accept={ALLOWED_TYPES.join(',')}
              disabled={uploading || loading}
              onChange={(event) => void handleUpload(event)}
            />
          </div>
        ) : null}

        {loading ? (
          <LoadingSpinner label="Cargando imágenes..." />
        ) : error ? (
          <AppAlert variant="danger" title="Error al cargar">
            {error}
          </AppAlert>
        ) : imagenes.length === 0 ? (
          <div className="text-center text-muted py-4">
            <i className="bi bi-image fs-2 d-block mb-2" aria-hidden="true" />
            <p className="mb-0">Este producto todavía no tiene imágenes.</p>
          </div>
        ) : (
          <div className="row g-3">
            {imagenes.map((imagen) => (
              <div className="col-6 col-md-4" key={imagen.id}>
                <div className="card h-100 border">
                  {/* eslint-disable-next-line @next/next/no-img-element -- URL pública del storage, sin optimización en runtime */}
                  <img
                    src={imagen.url}
                    alt={`${producto.nombre} · imagen ${imagen.orden + 1}`}
                    className="card-img-top object-fit-cover"
                    style={{ height: '10rem' }}
                  />
                  <div className="card-body d-flex justify-content-between align-items-center gap-2 p-2">
                    <span className="small text-muted">#{imagen.orden + 1}</span>
                    {canUpload ? (
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        title="Eliminar imagen"
                        disabled={uploading || deleting}
                        onClick={() => setConfirmTarget(imagen)}
                      >
                        <i className="bi bi-trash" aria-hidden="true" />
                      </button>
                    ) : null}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {uploading ? (
          <div className="text-center mt-3">
            <LoadingSpinner label="Subiendo imagen..." />
          </div>
        ) : null}
      </Modal>

      <ConfirmDialog
        open={confirmTarget !== null}
        title="Eliminar imagen"
        message={
          confirmTarget
            ? 'Se eliminará la imagen del almacenamiento y del catálogo. Esta acción no se puede deshacer.'
            : ''
        }
        confirmLabel="Eliminar"
        confirmVariant="danger"
        loading={deleting}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setConfirmTarget(null)}
      />
    </>
  );
}
