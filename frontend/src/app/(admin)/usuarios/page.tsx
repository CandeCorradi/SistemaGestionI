'use client';

/**
 * Gestión de usuarios internos (api-spec §9, BR-001/BR-003): listado
 * paginado con búsqueda, alta, edición parcial (PATCH) y asignación de
 * permisos explícitos adicionales al rol. La autorización real siempre
 * ocurre en backend (AGENTS.md §8).
 */

import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '@/features/auth/auth-provider';
import {
  createUsuario,
  getPermisosUsuario,
  listUsuarios,
  updatePermisosUsuario,
  updateUsuario,
} from '@/services/usuarios';
import { listRoles } from '@/services/roles';
import { listPermisosCatalogo } from '@/services/permisos';
import { fullName } from '@/lib/format';
import { ROL_LABELS, type PermisoCatalogo, type Rol, type UsuarioList } from '@/types/api';
import { PanelCard, StatCard } from '@/components/ui/cards';
import { DataTable } from '@/components/ui/data-table';
import { AppAlert } from '@/components/ui/app-alert';
import { AppButton } from '@/components/ui/app-button';
import { AppInput, AppSelect } from '@/components/ui/form-fields';
import { EmptyState } from '@/components/ui/empty-state';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Modal } from '@/components/ui/modal';
import { usePagedList } from '@/hooks/usePagedList';

interface CreateForm {
  email: string;
  nombre: string;
  apellido: string;
  rolId: string;
}

const EMPTY_CREATE: CreateForm = { email: '', nombre: '', apellido: '', rolId: '' };

interface EditForm {
  nombre: string;
  apellido: string;
  email: string;
  rolId: string;
  activo: boolean;
}

const MODULE_LABELS: Record<string, string> = {
  CLIENTES: 'Clientes',
  PROVEEDORES: 'Proveedores',
  PRODUCTOS: 'Productos',
  STOCK: 'Stock',
  PRESUPUESTOS: 'Presupuestos',
  PEDIDOS: 'Pedidos',
  PAGOS: 'Pagos',
  FACTURAS: 'Facturas',
  REPORTES: 'Reportes',
  USUARIOS: 'Usuarios',
  ROLES: 'Roles',
};

/** Agrupa los permisos del catálogo por módulo (prefijo del código). */
function groupByModule(permisos: PermisoCatalogo[]): Array<{ modulo: string; items: PermisoCatalogo[] }> {
  const map = new Map<string, PermisoCatalogo[]>();
  for (const permiso of permisos) {
    const modulo = permiso.codigo.split('_')[0] ?? 'OTROS';
    const list = map.get(modulo);
    if (list) {
      list.push(permiso);
    } else {
      map.set(modulo, [permiso]);
    }
  }
  return Array.from(map.entries()).map(([modulo, items]) => ({
    modulo,
    items: items.sort((a, b) => a.id - b.id),
  }));
}

export default function UsuariosPage() {
  const { can } = useAuth();

  const [roles, setRoles] = useState<Rol[]>([]);

  const {
    items,
    page,
    totalItems,
    totalPages,
    loading,
    error,
    forbidden,
    search,
    setSearch,
    setPage,
    reload,
  } = usePagedList<UsuarioList>({
    fetcher: (query, signal) =>
      listUsuarios(
        { page: query.page, pageSize: query.pageSize, search: query.search },
        signal
      ),
  });

  // Alta
  const [createOpen, setCreateOpen] = useState(false);
  const [createForm, setCreateForm] = useState<CreateForm>(EMPTY_CREATE);
  const [createError, setCreateError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  // Edición
  const [editUser, setEditUser] = useState<UsuarioList | null>(null);
  const [editForm, setEditForm] = useState<EditForm | null>(null);
  const [editError, setEditError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Permisos explícitos
  const [permsUser, setPermsUser] = useState<UsuarioList | null>(null);
  const [catalogo, setCatalogo] = useState<PermisoCatalogo[] | null>(null);
  const [selectedCodes, setSelectedCodes] = useState<Set<string>>(new Set());
  const [permsError, setPermsError] = useState<string | null>(null);
  const [permsLoading, setPermsLoading] = useState(false);
  const [permsSaving, setPermsSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();
    listRoles(controller.signal)
      .then((data) => {
        if (!cancelled) {
          setRoles(data);
        }
      })
      .catch(() => {
        // Los labels locales de respaldo (ROL_LABELS) ya cubren el dropdown.
      });
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, []);

  const roleOptions = useMemo(
    () => roles.map((rol) => ({ value: String(rol.id), label: rol.nombre })),
    [roles]
  );

  const groupedCatalog = useMemo(
    () => (catalogo ? groupByModule(catalogo) : []),
    [catalogo]
  );

  // ------------------------------------------------------------------
  // Alta
  // ------------------------------------------------------------------

  const onCreate = async () => {
    setCreateError(null);
    if (!createForm.email.trim() || !createForm.nombre.trim() || !createForm.rolId) {
      setCreateError('Email, nombre y rol son obligatorios.');
      return;
    }
    setCreating(true);
    try {
      await createUsuario({
        email: createForm.email.trim(),
        nombre: createForm.nombre.trim(),
        apellido: createForm.apellido.trim(),
        rolId: Number(createForm.rolId),
      });
      setCreateOpen(false);
      setCreateForm(EMPTY_CREATE);
      reload();
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'No se pudo crear el usuario.');
    } finally {
      setCreating(false);
    }
  };

  // ------------------------------------------------------------------
  // Edición
  // ------------------------------------------------------------------

  const onOpenEdit = (user: UsuarioList) => {
    setEditUser(user);
    setEditForm({
      nombre: user.nombre,
      apellido: user.apellido,
      email: user.email,
      rolId: String(user.rolId),
      activo: user.activo,
    });
    setEditError(null);
  };

  const onEdit = async () => {
    if (!editUser || !editForm) {
      return;
    }
    setEditError(null);
    setSaving(true);
    try {
      await updateUsuario(editUser.id, {
        nombre: editForm.nombre.trim(),
        apellido: editForm.apellido.trim(),
        email: editForm.email.trim(),
        rolId: Number(editForm.rolId),
        activo: editForm.activo,
      });
      setEditUser(null);
      setEditForm(null);
      reload();
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'No se pudo actualizar el usuario.');
    } finally {
      setSaving(false);
    }
  };

  // ------------------------------------------------------------------
  // Permisos explícitos
  // ------------------------------------------------------------------

  const onOpenPerms = async (user: UsuarioList) => {
    setPermsUser(user);
    setPermsError(null);
    setCatalogo(null);
    setSelectedCodes(new Set());
    setPermsLoading(true);
    try {
      const [catalogoData, usuarioPermisos] = await Promise.all([
        listPermisosCatalogo(),
        getPermisosUsuario(user.id),
      ]);
      setCatalogo(catalogoData);
      setSelectedCodes(new Set(usuarioPermisos.codigos));
    } catch (err) {
      setPermsError(err instanceof Error ? err.message : 'No se pudieron cargar los permisos.');
    } finally {
      setPermsLoading(false);
    }
  };

  const toggleCode = (codigo: string) => {
    setSelectedCodes((prev) => {
      const next = new Set(prev);
      if (next.has(codigo)) {
        next.delete(codigo);
      } else {
        next.add(codigo);
      }
      return next;
    });
  };

  const onSavePerms = async () => {
    if (!permsUser) {
      return;
    }
    setPermsError(null);
    setPermsSaving(true);
    try {
      await updatePermisosUsuario(permsUser.id, { codigos: Array.from(selectedCodes) });
      setPermsUser(null);
    } catch (err) {
      setPermsError(err instanceof Error ? err.message : 'No se pudieron guardar los permisos.');
    } finally {
      setPermsSaving(false);
    }
  };

  // ------------------------------------------------------------------

  if (!can('USUARIOS_VIEW')) {
    return (
      <AppAlert variant="warning" title="Sin permiso">
        Tu perfil no tiene el permiso USUARIOS_VIEW.
      </AppAlert>
    );
  }

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h1 className="h4 mb-1">Usuarios</h1>
          <p className="text-muted mb-0 small">
            Usuarios internos del sistema y sus permisos explícitos (BR-001: cinco roles fijos)
          </p>
        </div>
        {can('USUARIOS_CREATE') ? (
          <AppButton
            onClick={() => {
              setCreateForm(EMPTY_CREATE);
              setCreateError(null);
              setCreateOpen(true);
            }}
          >
            <i className="bi bi-person-plus me-1" aria-hidden="true" />
            Nuevo usuario
          </AppButton>
        ) : null}
      </div>

      <div className="row g-3 mb-4">
        <div className="col-12 col-md-4">
          <StatCard
            title="Usuarios registrados"
            value={totalItems || items.length}
            icon="bi-people"
            tone="bg-primary"
            footer={`Página ${page} de ${totalPages || 1}`}
          />
        </div>
      </div>

      <PanelCard
        title="Listado"
        actions={
          <div className="d-flex gap-2 align-items-center">
            <input
              type="search"
              className="form-control form-control-sm"
              placeholder="Buscar por email o nombre…"
              aria-label="Buscar usuarios"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            <AppButton variant="outline-secondary" className="btn-sm" onClick={reload}>
              <i className="bi bi-arrow-clockwise" aria-hidden="true" />
            </AppButton>
          </div>
        }
      >
        {forbidden ? (
          <AppAlert variant="warning" title="Sin permiso">
            Tu perfil no tiene permiso para ver los usuarios.
          </AppAlert>
        ) : loading ? (
          <LoadingSpinner label="Cargando usuarios..." />
        ) : error ? (
          <AppAlert variant="danger" title="Error al cargar">
            {error}
          </AppAlert>
        ) : items.length === 0 ? (
          <EmptyState
            icon="bi-people"
            title="Sin usuarios"
            description={
              search ? 'No hay resultados para la búsqueda.' : 'Todavía no hay usuarios cargados.'
            }
          />
        ) : (
          <>
            <DataTable
              columns={[
                { header: 'Email', render: (user) => user.email },
                {
                  header: 'Nombre',
                  render: (user) => fullName(user.nombre, user.apellido),
                },
                {
                  header: 'Rol',
                  render: (user) => (
                    <span className="badge text-bg-secondary">{ROL_LABELS[user.rolId] ?? `Rol ${user.rolId}`}</span>
                  ),
                },
                {
                  header: 'Estado',
                  render: (user) => (
                    <span className={`badge ${user.activo ? 'text-bg-success' : 'text-bg-danger'}`}>
                      {user.activo ? 'Activo' : 'Inactivo'}
                    </span>
                  ),
                },
              ]}
              rows={items}
              rowKey={(user) => user.id}
              actions={(user) => (
                <div className="d-flex justify-content-end gap-1">
                  {can('USUARIOS_UPDATE') ? (
                    <>
                      <AppButton
                        variant="outline-secondary"
                        className="btn-sm"
                        onClick={() => onOpenEdit(user)}
                      >
                        <i className="bi bi-pencil" aria-hidden="true" /> Editar
                      </AppButton>
                      <AppButton
                        variant="outline-secondary"
                        className="btn-sm"
                        onClick={() => void onOpenPerms(user)}
                      >
                        <i className="bi bi-shield-check" aria-hidden="true" /> Permisos
                      </AppButton>
                    </>
                  ) : null}
                </div>
              )}
            />
            {totalPages > 1 ? (
              <div className="d-flex justify-content-between align-items-center border-top p-2">
                <span className="small text-muted">
                  {totalItems} usuario{totalItems === 1 ? '' : 's'} en total
                </span>
                <div className="btn-group btn-group-sm">
                  <AppButton
                    variant="outline-secondary"
                    disabled={page <= 1}
                    onClick={() => setPage(page - 1)}
                  >
                    Anterior
                  </AppButton>
                  <AppButton
                    variant="outline-secondary"
                    disabled={page >= totalPages}
                    onClick={() => setPage(page + 1)}
                  >
                    Siguiente
                  </AppButton>
                </div>
              </div>
            ) : null}
          </>
        )}
      </PanelCard>

      {/* Modal: nuevo usuario */}
      <Modal
        open={createOpen}
        title="Nuevo usuario"
        onClose={() => setCreateOpen(false)}
        footer={
          <>
            <AppButton variant="outline-secondary" onClick={() => setCreateOpen(false)}>
              Cancelar
            </AppButton>
            <AppButton disabled={creating} onClick={() => void onCreate()}>
              {creating ? 'Creando…' : 'Crear usuario'}
            </AppButton>
          </>
        }
      >
        {createError ? (
          <AppAlert variant="danger" title="No se pudo crear">
            {createError}
          </AppAlert>
        ) : null}
        <AppInput
          label="Email"
          name="usuario-email"
          type="email"
          required
          value={createForm.email}
          onChange={(event) => setCreateForm({ ...createForm, email: event.target.value })}
        />
        <div className="row g-3">
          <div className="col-6">
            <AppInput
              label="Nombre"
              name="usuario-nombre"
              required
              value={createForm.nombre}
              onChange={(event) => setCreateForm({ ...createForm, nombre: event.target.value })}
            />
          </div>
          <div className="col-6">
            <AppInput
              label="Apellido"
              name="usuario-apellido"
              value={createForm.apellido}
              onChange={(event) => setCreateForm({ ...createForm, apellido: event.target.value })}
            />
          </div>
        </div>
        <AppSelect
          label="Rol"
          name="usuario-rol"
          required
          placeholder="Seleccioná un rol…"
          options={roleOptions}
          value={createForm.rolId}
          onChange={(event) => setCreateForm({ ...createForm, rolId: event.target.value })}
        />
        <p className="small text-muted mb-0">
          El usuario nace sin vínculo Firebase: se activa al iniciar sesión por primera vez.
        </p>
      </Modal>

      {/* Modal: editar usuario */}
      <Modal
        open={editUser !== null}
        title={`Editar: ${editUser ? editUser.email : ''}`}
        onClose={() => {
          setEditUser(null);
          setEditForm(null);
        }}
        footer={
          <>
            <AppButton
              variant="outline-secondary"
              onClick={() => {
                setEditUser(null);
                setEditForm(null);
              }}
            >
              Cancelar
            </AppButton>
            <AppButton disabled={saving} onClick={() => void onEdit()}>
              {saving ? 'Guardando…' : 'Guardar cambios'}
            </AppButton>
          </>
        }
      >
        {editError ? (
          <AppAlert variant="danger" title="No se pudo actualizar">
            {editError}
          </AppAlert>
        ) : null}
        {editForm ? (
          <>
            <AppInput
              label="Email"
              name="editar-email"
              type="email"
              required
              value={editForm.email}
              onChange={(event) => setEditForm({ ...editForm, email: event.target.value })}
            />
            <div className="row g-3">
              <div className="col-6">
                <AppInput
                  label="Nombre"
                  name="editar-nombre"
                  required
                  value={editForm.nombre}
                  onChange={(event) => setEditForm({ ...editForm, nombre: event.target.value })}
                />
              </div>
              <div className="col-6">
                <AppInput
                  label="Apellido"
                  name="editar-apellido"
                  value={editForm.apellido}
                  onChange={(event) => setEditForm({ ...editForm, apellido: event.target.value })}
                />
              </div>
            </div>
            <AppSelect
              label="Rol"
              name="editar-rol"
              required
              options={roleOptions}
              value={editForm.rolId}
              onChange={(event) => setEditForm({ ...editForm, rolId: event.target.value })}
            />
            <div className="form-check form-switch">
              <input
                className="form-check-input"
                type="checkbox"
                id="editar-activo"
                checked={editForm.activo}
                onChange={(event) => setEditForm({ ...editForm, activo: event.target.checked })}
              />
              <label className="form-check-label" htmlFor="editar-activo">
                Cuenta activa
              </label>
            </div>
          </>
        ) : null}
      </Modal>

      {/* Modal: permisos explícitos */}
      <Modal
        open={permsUser !== null}
        size="modal-lg"
        title={`Permisos explícitos: ${permsUser ? permsUser.email : ''}`}
        onClose={() => setPermsUser(null)}
        footer={
          <>
            <AppButton variant="outline-secondary" onClick={() => setPermsUser(null)}>
              Cancelar
            </AppButton>
            <AppButton
              disabled={permsSaving || permsLoading}
              onClick={() => void onSavePerms()}
            >
              {permsSaving ? 'Guardando…' : 'Guardar permisos'}
            </AppButton>
          </>
        }
      >
        {permsError ? (
          <AppAlert variant="danger" title="Error">
            {permsError}
          </AppAlert>
        ) : null}
        <p className="small text-muted">
          Estos permisos se <strong>suman</strong> a los de su rol. El rol ya otorga permisos
          base (BR-001); acá se registran sólo las excepciones.
        </p>
        {permsLoading || !catalogo ? (
          <LoadingSpinner label="Cargando catálogo de permisos..." />
        ) : (
          groupedCatalog.map((grupo) => (
            <div key={grupo.modulo} className="mb-3">
              <div className="fw-semibold small text-uppercase text-muted mb-1">
                {MODULE_LABELS[grupo.modulo] ?? grupo.modulo}
              </div>
              <div className="d-flex flex-wrap gap-3">
                {grupo.items.map((permiso) => (
                  <div className="form-check" key={permiso.id}>
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id={`permiso-${permiso.id}`}
                      checked={selectedCodes.has(permiso.codigo)}
                      onChange={() => toggleCode(permiso.codigo)}
                    />
                    <label className="form-check-label small" htmlFor={`permiso-${permiso.id}`}>
                      {permiso.nombre}{' '}
                      <span className="text-muted">({permiso.codigo})</span>
                    </label>
                  </div>
                ))}
              </div>
            </div>
          ))
        )}
        <p className="small text-muted mb-0">
          Seleccionados: {selectedCodes.size} de {catalogo?.length ?? 0}
        </p>
      </Modal>
    </>
  );
}
