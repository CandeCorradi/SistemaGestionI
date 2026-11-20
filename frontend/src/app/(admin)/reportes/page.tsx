'use client';

/**
 * Reportes operativos (api-spec §20, functional-spec §21, BR-140/BR-141):
 * preview en tabla + exportación XLSX/CSV/PDF. El backend es la única
 * autoridad de totales; el frontend sólo presenta el documento y maneja
 * la descarga.
 */

import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import { downloadReport, getReport, saveBlob } from '@/services/reportes';
import { formatDateTime } from '@/lib/format';
import {
  ESTADO_PEDIDO_LABELS,
  ESTADO_PRESUPUESTO_LABELS,
  METODO_PAGO_LABELS,
  REPORT_FORMAT_LABELS,
  REPORT_KEY_LABELS,
  TIPO_PRODUCTO_LABELS,
  type EstadoPedido,
  type EstadoPresupuesto,
  type MetodoPago,
  type ReportCellValue,
  type ReportDocument,
  type ReportFormat,
  type ReportKey,
  type ReportQueryParams,
  type ReportRow,
  type TipoProducto,
} from '@/types/api';
import { PanelCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput } from '@/components/ui/form-fields';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';

const REPORT_KEYS: ReportKey[] = ['orders', 'payments', 'quotes', 'sales', 'inventory'];

function formatCell(value: ReportCellValue): string {
  if (value === null || value === undefined) {
    return '—';
  }
  if (typeof value === 'boolean') {
    return value ? 'Sí' : 'No';
  }
  if (typeof value === 'number') {
    return Number.isInteger(value) ? String(value) : value.toFixed(2);
  }
  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
    return formatDateTime(value);
  }
  return value;
}

export default function ReportesPage() {
  const { can } = useAuth();

  const [key, setKey] = useState<ReportKey>('orders');
  const [draftFrom, setDraftFrom] = useState('');
  const [draftTo, setDraftTo] = useState('');
  const [draftStatus, setDraftStatus] = useState('');
  const [draftMethod, setDraftMethod] = useState('');
  const [draftType, setDraftType] = useState('');
  const [applied, setApplied] = useState<{ key: ReportKey; params: ReportQueryParams }>({
    key: 'orders',
    params: {},
  });

  const [document, setDocument] = useState<ReportDocument | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState<ReportFormat | null>(null);
  const [exportError, setExportError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();

    const load = async () => {
      try {
        const doc = await getReport(applied.key, applied.params, controller.signal);
        if (!cancelled) {
          setDocument(doc);
          setError(null);
        }
      } catch (err) {
        if (!cancelled && (err as { name?: string }).name !== 'AbortError') {
          setError(err instanceof Error ? err.message : 'No se pudo cargar el reporte.');
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [applied]);

  // Primera carga (sin documento ni error todavía).
  const firstLoad = document === null && error === null;

  const onApply = useCallback(() => {
    const params: ReportQueryParams = {};
    if (draftFrom) {
      params.from = draftFrom;
    }
    if (draftTo) {
      params.to = draftTo;
    }
    if (draftStatus) {
      params.status = Number(draftStatus);
    }
    if (draftMethod) {
      params.method = Number(draftMethod);
    }
    if (draftType) {
      params.type = Number(draftType);
    }
    setApplied({ key, params });
  }, [draftFrom, draftTo, draftStatus, draftMethod, draftType, key]);

  const onExport = async (format: ReportFormat) => {
    setExportError(null);
    setExporting(format);
    try {
      const file = await downloadReport(applied.key, applied.params, format);
      saveBlob(file.blob, file.fileName);
    } catch (err) {
      setExportError(err instanceof Error ? err.message : 'No se pudo exportar el reporte.');
    } finally {
      setExporting(null);
    }
  };

  if (!can('REPORTES_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        Tu perfil no tiene el permiso REPORTES_VIEW.
      </AppAlert>
    );
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Reportes</h1>
          <p className="text-muted mb-0 small">
            Exportación a XLSX, CSV y PDF (BR-140: visualizar con REPORTES_VIEW, exportar con
            REPORTES_EXPORT)
          </p>
        </div>
        {can('REPORTES_EXPORT') ? (
          <div className="d-flex flex-wrap gap-2">
            {(Object.keys(REPORT_FORMAT_LABELS) as ReportFormat[]).map((format) => (
              <AppButton
                key={format}
                variant="outline-secondary"
                disabled={exporting !== null || firstLoad}
                onClick={() => void onExport(format)}
              >
                {exporting === format ? 'Exportando…' : `Exportar ${format.toUpperCase()}`}
              </AppButton>
            ))}
          </div>
        ) : null}
      </div>

      {exportError ? (
        <AppAlert variant="danger" title="Error al exportar">
          {exportError}
        </AppAlert>
      ) : null}

      <PanelCard title="Filtros">
        <div className="row g-3 align-items-end">
          <div className="col-12 col-md-3">
            <label className="form-label small" htmlFor="reporte-tipo">
              Reporte
            </label>
            <select
              id="reporte-tipo"
              className="form-select"
              value={key}
              onChange={(event) => {
                setKey(event.target.value as ReportKey);
                setDraftStatus('');
                setDraftMethod('');
                setDraftType('');
              }}
            >
              {REPORT_KEYS.map((reportKey) => (
                <option key={reportKey} value={reportKey}>
                  {REPORT_KEY_LABELS[reportKey]}
                </option>
              ))}
            </select>
          </div>
          <div className="col-6 col-md-2">
            <AppInput
              label="Desde"
              name="reporte-desde"
              type="date"
              value={draftFrom}
              onChange={(event) => setDraftFrom(event.target.value)}
            />
          </div>
          <div className="col-6 col-md-2">
            <AppInput
              label="Hasta"
              name="reporte-hasta"
              type="date"
              value={draftTo}
              onChange={(event) => setDraftTo(event.target.value)}
            />
          </div>

          {key === 'orders' || key === 'quotes' ? (
            <div className="col-6 col-md-2">
              <label className="form-label small" htmlFor="reporte-estado">
                Estado
              </label>
              <select
                id="reporte-estado"
                className="form-select"
                value={draftStatus}
                onChange={(event) => setDraftStatus(event.target.value)}
              >
                <option value="">Todos</option>
                {(key === 'orders'
                  ? (Object.keys(ESTADO_PEDIDO_LABELS) as string[])
                  : (Object.keys(ESTADO_PRESUPUESTO_LABELS) as string[])
                ).map((value) => (
                  <option key={value} value={value}>
                    {key === 'orders'
                      ? ESTADO_PEDIDO_LABELS[Number(value) as EstadoPedido]
                      : ESTADO_PRESUPUESTO_LABELS[Number(value) as EstadoPresupuesto]}
                  </option>
                ))}
              </select>
            </div>
          ) : null}

          {key === 'payments' ? (
            <div className="col-6 col-md-2">
              <label className="form-label small" htmlFor="reporte-metodo">
                Método de pago
              </label>
              <select
                id="reporte-metodo"
                className="form-select"
                value={draftMethod}
                onChange={(event) => setDraftMethod(event.target.value)}
              >
                <option value="">Todos</option>
                {(Object.keys(METODO_PAGO_LABELS) as string[]).map((value) => (
                  <option key={value} value={value}>
                    {METODO_PAGO_LABELS[Number(value) as MetodoPago]}
                  </option>
                ))}
              </select>
            </div>
          ) : null}

          {key === 'inventory' ? (
            <div className="col-6 col-md-2">
              <label className="form-label small" htmlFor="reporte-tipo-producto">
                Tipo de producto
              </label>
              <select
                id="reporte-tipo-producto"
                className="form-select"
                value={draftType}
                onChange={(event) => setDraftType(event.target.value)}
              >
                <option value="">Todos</option>
                {(Object.keys(TIPO_PRODUCTO_LABELS) as string[]).map((value) => (
                  <option key={value} value={value}>
                    {TIPO_PRODUCTO_LABELS[Number(value) as TipoProducto]}
                  </option>
                ))}
              </select>
            </div>
          ) : null}

          <div className="col-12 col-md-3">
            <AppButton onClick={onApply}>
              <i className="bi bi-funnel me-1" aria-hidden="true" />
              Aplicar
            </AppButton>
          </div>
        </div>
      </PanelCard>

      {firstLoad ? (
        <LoadingSpinner label="Cargando reporte..." />
      ) : error ? (
        <AppAlert variant="danger" title="Error al cargar">
          {error}
        </AppAlert>
      ) : !document || document.rows.length === 0 ? (
        <EmptyState
          icon="bi-bar-chart-line"
          title="Sin datos"
          description="No hay resultados para los filtros seleccionados."
        />
      ) : (
        <PanelCard
          title={document.title}
          actions={
            <span className="small text-muted">
              {document.rows.length} fila{document.rows.length === 1 ? '' : 's'} · generado{' '}
              {formatDateTime(document.generatedAtUtc)}
            </span>
          }
        >
          {document.appliedFilters.length > 0 ? (
            <div className="px-3 pt-2 d-flex flex-wrap gap-2">
              {document.appliedFilters.map((filter) => (
                <span key={filter.key} className="badge text-bg-light border small">
                  {filter.key}: {filter.value}
                </span>
              ))}
            </div>
          ) : null}
          <DataTable
            columns={document.columns.map((column) => ({
              header: column.header,
              className:
                column.align === 'right'
                  ? 'text-end'
                  : column.align === 'center'
                    ? 'text-center'
                    : undefined,
              render: (row: ReportRow) => formatCell(row.cells[column.key]),
            }))}
            rows={document.rows}
            rowKey={(_, index) => index}
          />
          {document.summary.length > 0 ? (
            <div className="border-top p-3 d-flex flex-wrap gap-4">
              {document.summary.map((item) => (
                <div key={item.label}>
                  <div className="small text-muted">{item.label}</div>
                  <div className="fw-semibold">{formatCell(item.value)}</div>
                </div>
              ))}
            </div>
          ) : null}
        </PanelCard>
      )}
    </>
  );
}
