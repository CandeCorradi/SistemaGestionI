'use client';

import type { ReactNode } from 'react';

interface Column<T> {
  header: string;
  render: (row: T) => ReactNode;
  /** Clases de alineación (ej. 'text-end'). */
  className?: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  /** Clave de la fila; el índice está disponible como respaldo (listas genéricas). */
  rowKey: (row: T, index: number) => number | string;
  /** Acciones por fila (última columna). */
  actions?: (row: T) => ReactNode;
}

/** Tabla de Bootstrap reutilizable para listados (roadmap §17). */
export function DataTable<T>({ columns, rows, rowKey, actions }: DataTableProps<T>) {
  return (
    <div className="table-responsive">
      <table className="table table-hover align-middle mb-0">
        <thead className="table-light">
          <tr>
            {columns.map((column) => (
              <th key={column.header} scope="col" className={column.className}>
                {column.header}
              </th>
            ))}
            {actions ? (
              <th scope="col" className="text-end">
                Acciones
              </th>
            ) : null}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={rowKey(row, index)}>
              {columns.map((column) => (
                <td key={column.header} className={column.className}>
                  {column.render(row)}
                </td>
              ))}
              {actions ? <td className="text-end">{actions(row)}</td> : null}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
