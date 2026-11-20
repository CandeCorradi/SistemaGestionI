using SistemaGestion.Application.TrabajosRealizados;
using SistemaGestion.Application.TrabajosRealizados.Dtos;
using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

/// <summary>
/// Tests de casos de uso de trabajos realizados / vidriera (AGENTS.md
/// §23, BR-190/BR-191, AGENTS.md §40): publicación, privacidad del
/// cliente, etiquetas e imágenes en storage externo.
/// </summary>
public sealed class TrabajosRealizadosServiceTests
{
    private const string DefaultContentType = "image/png";

    // ------------------------------------------------------------------
    // CRUD — validaciones
    // ------------------------------------------------------------------

    [Fact]
    public async Task Create_WithoutNombre_ReturnsValidation()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.CreateAsync(new CreateTrabajoRealizadoDto("   "));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithTooLongNombre_ReturnsValidation()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.CreateAsync(new CreateTrabajoRealizadoDto(new string('x', 201)));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithTooManyTags_ReturnsValidation()
    {
        var ctx = CreateContext();
        var tags = Enumerable.Range(1, TrabajoRealizadoMaxEtiquetas + 1).Select(i => $"tag{i}").ToList();

        var result = await ctx.Service.CreateAsync(new CreateTrabajoRealizadoDto("Trabajo", Tags: tags));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
    }

    [Fact]
    public async Task Create_NormalizesTags_AndTrimsNombre()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.CreateAsync(new CreateTrabajoRealizadoDto(
            "  Cocina  ",
            Tags: ["Cocina", "cocina", " ISLA ", ""]));

        Assert.True(result.IsSuccess);
        Assert.Equal("Cocina", result.Value.Nombre);
        Assert.Equal(["Cocina", "isla"], result.Value.Tags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_DefaultsAreUnpublishedAndNotFeatured()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.CreateAsync(new CreateTrabajoRealizadoDto("Trabajo Nuevo"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Publicado);
        Assert.False(result.Value.Destacado);
    }

    [Fact]
    public async Task Update_PatchOnlyChangesProvidedFields()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed(
            nombre: "Original",
            tipoMueble: "Cocina",
            clienteNombre: "Laura",
            categoria: "Cocinas");

        var result = await ctx.Service.UpdateAsync(
            trabajo.Id, new UpdateTrabajoRealizadoDto(Nombre: "Renombrado"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Renombrado", result.Value.Nombre);
        Assert.Equal("Cocina", result.Value.TipoMueble);
        Assert.Equal("Laura", result.Value.ClienteNombre);
        Assert.Equal("Cocinas", result.Value.Categoria);
    }

    [Fact]
    public async Task Update_EmptiedOptionalField_ClearsValue()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed(clienteNombre: "Laura");

        var result = await ctx.Service.UpdateAsync(
            trabajo.Id, new UpdateTrabajoRealizadoDto(ClienteNombre: string.Empty));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ClienteNombre);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.UpdateAsync(999, new UpdateTrabajoRealizadoDto(Nombre: "X"));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Delete_RemovesWork()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed();

        var result = await ctx.Service.DeleteAsync(trabajo.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(ctx.Repo.All);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var ctx = CreateContext();

        var result = await ctx.Service.DeleteAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Vidriera pública — BR-190 / BR-191
    // ------------------------------------------------------------------

    [Fact]
    public async Task ShowroomList_ExcludesUnpublished()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed(nombre: "Publicado", publicado: true);
        ctx.Repo.Seed(nombre: "Borrador", publicado: false);

        var result = await ctx.Service.GetShowroomPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items, i => i.Nombre == "Publicado");
        Assert.DoesNotContain(result.Value.Items, i => i.Nombre == "Borrador");
    }

    [Fact]
    public async Task ShowroomList_HidesClienteNombre_WhenNotPublicado()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed(nombre: "Obra A", clienteNombre: "Laura Gómez", publicarNombreCliente: false);
        ctx.Repo.Seed(nombre: "Obra B", clienteNombre: "Martín Pérez", publicarNombreCliente: true);

        var result = await ctx.Service.GetShowroomPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Items.Single(i => i.Nombre == "Obra A").ClienteNombre);
        Assert.Equal(
            "Martín Pérez", result.Value.Items.Single(i => i.Nombre == "Obra B").ClienteNombre);
    }

    [Fact]
    public async Task ShowroomList_FeaturedFirst()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed(nombre: "Normal", destacado: false, fecha: new DateOnly(2026, 6, 1));
        ctx.Repo.Seed(nombre: "Destacado", destacado: true, fecha: new DateOnly(2026, 1, 1));

        var result = await ctx.Service.GetShowroomPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Destacado", result.Value.Items.First().Nombre);
    }

    [Fact]
    public async Task ShowroomList_WithPortada_ReturnsFirstImageUrl()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed();
        ctx.Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/b.jpg", orden: 1);
        ctx.Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/a.jpg", orden: 0);

        var result = await ctx.Service.GetShowroomPagedAsync(1, 20, null, null, null);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal($"{ctx.Storage.BaseUrl}/trabajos/{trabajo.Id}/a.jpg", item.ImagenUrl);
    }

    [Fact]
    public async Task ShowroomList_DoesNotExposeInternalFields()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed();

        _ = await ctx.Service.GetShowroomPagedAsync(1, 20, null, null, null);

        var propiedades = typeof(ShowroomItemDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("Publicado", propiedades);
        Assert.DoesNotContain("PublicarNombreCliente", propiedades);
        Assert.DoesNotContain("FechaCreacion", propiedades);
        Assert.DoesNotContain("FechaActualizacion", propiedades);
    }

    [Fact]
    public async Task ShowroomById_Unpublished_ReturnsNotFound()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed(publicado: false);

        var result = await ctx.Service.GetShowroomByIdAsync(trabajo.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task ShowroomById_HidesClienteNombre_WhenNotPublicado()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed(clienteNombre: "Laura", publicarNombreCliente: false);

        var result = await ctx.Service.GetShowroomByIdAsync(trabajo.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ClienteNombre);
    }

    [Fact]
    public async Task ShowroomById_ShowsClienteNombre_WhenPublicado()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed(clienteNombre: "Laura", publicarNombreCliente: true);

        var result = await ctx.Service.GetShowroomByIdAsync(trabajo.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Laura", result.Value.ClienteNombre);
    }

    [Fact]
    public async Task AdminList_IncludesUnpublished()
    {
        var ctx = CreateContext();
        ctx.Repo.Seed(nombre: "Publicado", publicado: true);
        ctx.Repo.Seed(nombre: "Borrador", publicado: false);

        var result = await ctx.Service.GetListAsync(1, 20, null, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
    }

    // ------------------------------------------------------------------
    // Imágenes — patrón storage (BR-100/BR-101, AGENTS.md §24)
    // ------------------------------------------------------------------

    [Fact]
    public async Task AddImage_UploadsAndRegistersReference()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed();

        var result = await ctx.Service.AddImageAsync(
            trabajo.Id, "foto.png", DefaultContentType, NewStream(), 3);

        Assert.True(result.IsSuccess);
        Assert.StartsWith($"{ctx.Storage.BaseUrl}/trabajos/{trabajo.Id}/", result.Value.Url);
        Assert.Single(ctx.Imagenes.All);
        Assert.Single(ctx.Storage.UploadedPaths);
    }

    [Fact]
    public async Task AddImage_WithInvalidExtension_ReturnsValidation()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed();

        var result = await ctx.Service.AddImageAsync(
            trabajo.Id, "archivo.exe", "application/octet-stream", NewStream(), 3);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Error", result.Error.Code);
        Assert.Empty(ctx.Storage.UploadedPaths);
    }

    [Fact]
    public async Task AddImage_WhenPersistenciaFalla_LimpiaHuérfano()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed();
        ctx.Imagenes.ThrowOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.Service.AddImageAsync(
            trabajo.Id, "foto.png", DefaultContentType, NewStream(), 3));

        Assert.Single(ctx.Storage.UploadedPaths);
        Assert.Single(ctx.Storage.DeletedPaths);
    }

    [Fact]
    public async Task DeleteImage_RemovesStorageObjectAndReference()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed();
        var imagen = ctx.Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/a.jpg");

        var result = await ctx.Service.DeleteImageAsync(trabajo.Id, imagen.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(ctx.Imagenes.All);
        Assert.Equal($"trabajos/{trabajo.Id}/a.jpg", Assert.Single(ctx.Storage.DeletedPaths));
    }

    [Fact]
    public async Task DeleteImage_FromAnotherWork_ReturnsNotFound()
    {
        var ctx = CreateContext();
        var trabajoA = ctx.Repo.Seed();
        var trabajoB = ctx.Repo.Seed();
        var imagenB = ctx.Imagenes.Seed(trabajoB.Id);

        var result = await ctx.Service.DeleteImageAsync(trabajoA.Id, imagenB.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Error", result.Error.Code);
    }

    [Fact]
    public async Task Delete_DeletesImageReferences_AndBestEffortStorage()
    {
        var ctx = CreateContext();
        var trabajo = ctx.Repo.Seed();
        var imagen = ctx.Imagenes.Seed(trabajo.Id, $"trabajos/{trabajo.Id}/a.jpg");

        var result = await ctx.Service.DeleteAsync(trabajo.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(ctx.Imagenes.All);
        Assert.Equal(imagen.Path, Assert.Single(ctx.Storage.DeletedPaths));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static MemoryStream NewStream() => new(new byte[] { 1, 2, 3 });

    private const int TrabajoRealizadoMaxEtiquetas = 10;

    private static Context CreateContext()
    {
        var repo = new FakeTrabajoRealizadoRepository();
        var imagenes = new FakeTrabajoRealizadoImagenRepository();
        var storage = new FakeStorageService();
        return new Context(
            new TrabajosRealizadosService(repo, imagenes, storage), repo, imagenes, storage);
    }

    private sealed record Context(
        TrabajosRealizadosService Service,
        FakeTrabajoRealizadoRepository Repo,
        FakeTrabajoRealizadoImagenRepository Imagenes,
        FakeStorageService Storage);
}
