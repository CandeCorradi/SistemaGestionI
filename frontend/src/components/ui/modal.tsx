'use client';

import { useEffect, type ReactNode } from 'react';

interface ModalProps {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  size?: 'modal-lg' | 'modal-xl';
}

/**
 * Modal accesible de Bootstrap sin dependencias de JS externo: backdrop,
 * cierre con ESC y clic fuera, foco en el diálogo (roadmap §17).
 */
export function Modal({ open, title, onClose, children, footer, size }: ModalProps) {
  useEffect(() => {
    if (!open) {
      return;
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };
    document.addEventListener('keydown', onKeyDown);
    document.body.classList.add('modal-open');
    return () => {
      document.removeEventListener('keydown', onKeyDown);
      document.body.classList.remove('modal-open');
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <>
      {/* Backdrop fuera del role="dialog" y con position fixed para que
          cubra todo el viewport y no intercepte los controles del diálogo
          en modales más altos que la ventana (afectaba a los tests E2E). */}
      <div className="modal-backdrop fade show" aria-hidden="true" />
      <div
        className="modal fade show d-block"
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        onMouseDown={(event) => {
          if (event.target === event.currentTarget) {
            onClose();
          }
        }}
      >
        <div className={`modal-dialog ${size ?? ''}`.trim()}>
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title" id="modal-title">
                {title}
              </h5>
              <button type="button" className="btn-close" aria-label="Cerrar" onClick={onClose} />
            </div>
            {/* max-height + scroll: los modales altos quedan usables en
                pantallas chicas y el footer siempre es alcanzable. */}
            <div className="modal-body" style={{ maxHeight: '70vh', overflowY: 'auto' }}>
              {children}
            </div>
            {footer ? <div className="modal-footer">{footer}</div> : null}
          </div>
        </div>
      </div>
    </>
  );
}
