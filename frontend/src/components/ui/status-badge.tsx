'use client';

export function StatusBadge({ active }: { active: boolean }) {
  return active ? (
    <span className="badge text-bg-success">
      <i className="bi bi-check-lg me-1" aria-hidden="true" />
      Activo
    </span>
  ) : (
    <span className="badge text-bg-secondary">
      <i className="bi bi-x-lg me-1" aria-hidden="true" />
      Inactivo
    </span>
  );
}
