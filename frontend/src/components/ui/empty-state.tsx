'use client';

interface EmptyStateProps {
  icon?: string;
  title: string;
  description?: string;
  action?: React.ReactNode;
}

/** Estado vacío de listados (roadmap §17, AGENTS.md §35). */
export function EmptyState({ icon = 'bi-inbox', title, description, action }: EmptyStateProps) {
  return (
    <div className="text-center py-5 px-3">
      <i className={`bi ${icon} display-6 text-body-secondary`} aria-hidden="true" />
      <h6 className="mt-3 mb-1">{title}</h6>
      {description ? <p className="text-muted small mb-0">{description}</p> : null}
      {action ? <div className="mt-3">{action}</div> : null}
    </div>
  );
}
