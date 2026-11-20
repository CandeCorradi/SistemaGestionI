'use client';

/** Indicador de carga reutilizable (roadmap §17). */
export function LoadingSpinner({ label = 'Cargando...' }: { label?: string }) {
  return (
    <div className="d-flex flex-column align-items-center justify-content-center py-5" role="status">
      <div className="spinner-border text-primary" aria-hidden="true" />
      <span className="mt-2 text-muted small">{label}</span>
    </div>
  );
}
