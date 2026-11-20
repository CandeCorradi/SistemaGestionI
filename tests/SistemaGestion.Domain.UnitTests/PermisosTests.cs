using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Entities;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

/// <summary>
/// Catálogo fijo de permisos granulares (AGENTS.md §7, BR-002).
/// </summary>
public sealed class PermisosTests
{
    [Fact]
    public void Catalogo_HasStableIds_AndUniqueCodes()
    {
        Assert.Equal(41, Permisos.Catalogo.Count);
        Assert.Equal(Permisos.Catalogo.Count, Permisos.Catalogo.Select(p => p.Id).Distinct().Count());
        Assert.Equal(Permisos.Catalogo.Count, Permisos.Catalogo.Select(p => p.Codigo).Distinct().Count());
        Assert.All(Permisos.Catalogo, p => Assert.True(p.Id > 0));
    }

    [Fact]
    public void Catalogo_CodesFollowUpperSnakeFormat()
    {
        Assert.All(Permisos.Catalogo, p =>
            Assert.Matches("^[A-Z][A-Z0-9_]*$", p.Codigo));
    }

    [Fact]
    public void Todos_MatchesCatalogCodes()
    {
        Assert.Equal(
            Permisos.Catalogo.Select(p => p.Codigo),
            Permisos.Todos);
    }

    [Theory]
    [InlineData(Permisos.ClientesView)]
    [InlineData(Permisos.ClientesCreate)]
    [InlineData(Permisos.PresupuestosView)]
    [InlineData(Permisos.PresupuestosCreate)]
    [InlineData(Permisos.UsuariosView)]
    [InlineData(Permisos.UsuariosCreate)]
    [InlineData(Permisos.UsuariosUpdate)]
    [InlineData(Permisos.UsuariosDelete)]
    [InlineData(Permisos.RolesView)]
    [InlineData(Permisos.RolesUpdate)]
    [InlineData(Permisos.TrabajosView)]
    [InlineData(Permisos.TrabajosCreate)]
    [InlineData(Permisos.TrabajosUpdate)]
    [InlineData(Permisos.TrabajosDelete)]
    public void Constants_BelongToCatalog(string codigo)
        => Assert.Contains(codigo, Permisos.Todos);

    [Fact]
    public void Catalogo_CoversEveryPermissionGroup()
    {
        var prefijos = Permisos.Catalogo
            .Select(p => p.Codigo.Split('_')[0])
            .Distinct()
            .ToList();

        Assert.Contains("CLIENTES", prefijos);
        Assert.Contains("PROVEEDORES", prefijos);
        Assert.Contains("PRODUCTOS", prefijos);
        Assert.Contains("STOCK", prefijos);
        Assert.Contains("PRESUPUESTOS", prefijos);
        Assert.Contains("PEDIDOS", prefijos);
        Assert.Contains("PAGOS", prefijos);
        Assert.Contains("FACTURAS", prefijos);
        Assert.Contains("REPORTES", prefijos);
        Assert.Contains("USUARIOS", prefijos);
        Assert.Contains("ROLES", prefijos);
        Assert.Contains("TRABAJOS", prefijos);
    }
}

/// <summary>
/// Entidades de asociación rol ↔ permiso y usuario ↔ permiso.
/// </summary>
public sealed class RolPermisoTests
{
    [Fact]
    public void Crear_WithValidIds_StoresKeys()
    {
        var rolPermiso = new RolPermiso(4, 17);

        Assert.Equal(4, rolPermiso.RolId);
        Assert.Equal(17, rolPermiso.PermisoId);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(4, 0)]
    [InlineData(4, -1)]
    public void Crear_WithInvalidIds_Throws(int rolId, int permisoId)
        => Assert.Throws<ArgumentException>(() => new RolPermiso(rolId, permisoId));
}

public sealed class UsuarioPermisoTests
{
    [Fact]
    public void Crear_WithValidIds_StoresKeys()
    {
        var usuarioPermiso = new UsuarioPermiso(7, 13);

        Assert.Equal(7, usuarioPermiso.UsuarioId);
        Assert.Equal(13, usuarioPermiso.PermisoId);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(7, 0)]
    [InlineData(7, -1)]
    public void Crear_WithInvalidIds_Throws(int usuarioId, int permisoId)
        => Assert.Throws<ArgumentException>(() => new UsuarioPermiso(usuarioId, permisoId));
}

public sealed class PermisoTests
{
    [Fact]
    public void Crear_TrimsAndStoresFields()
    {
        var permiso = new Permiso(" CLIENTES_VIEW ", " Ver clientes ");

        Assert.Equal("CLIENTES_VIEW", permiso.Codigo);
        Assert.Equal("Ver clientes", permiso.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithoutCodigo_Throws(string codigo)
        => Assert.Throws<ArgumentException>(() => new Permiso(codigo, "Ver clientes"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithoutNombre_Throws(string nombre)
        => Assert.Throws<ArgumentException>(() => new Permiso("CLIENTES_VIEW", nombre));
}
