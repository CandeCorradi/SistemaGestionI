using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.TrabajosRealizados.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.TrabajosRealizados;

/// <summary>
/// Casos de uso de trabajos realizados / vidriera (AGENTS.md §23,
/// roadmap §13): alta, consulta administrativa, publicación y
/// exhibición pública. Sólo los trabajos publicados aparecen en el
/// showroom (BR-190); el nombre del cliente se oculta salvo consentimiento
/// (BR-191); las imágenes viven en el storage externo (BR-100/BR-101).
/// </summary>
public sealed class TrabajosRealizadosService
{
    /// <summary>
    /// Tamaño de página máximo permitido (AGENTS.md §30: sin páginas ilimitadas).
    /// </summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    /// <summary>Tamaño máximo de una imagen de trabajo (bytes).</summary>
    public const long MaxImageBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedImageExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private static readonly string[] AllowedImageContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private readonly ITrabajoRealizadoRepository _repository;

    private readonly ITrabajoRealizadoImagenRepository _imagenes;

    private readonly IStorageService _storage;

    public TrabajosRealizadosService(
        ITrabajoRealizadoRepository repository,
        ITrabajoRealizadoImagenRepository imagenes,
        IStorageService storage)
    {
        _repository = repository;
        _imagenes = imagenes;
        _storage = storage;
    }

    // ------------------------------------------------------------------
    // Administración
    // ------------------------------------------------------------------

    public async Task<Result<PagedResult<TrabajoRealizadoListDto>>> GetListAsync(
        int page,
        int pageSize,
        string? search,
        bool? publicado,
        bool? destacado,
        string? categoria,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();

        var paged = await _repository.GetPagedAsync(
            page, pageSize, search, publicado, destacado, categoria, cancellationToken);

        return new PagedResult<TrabajoRealizadoListDto>
        {
            Items = paged.Items.Select(TrabajoRealizadoListDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    public async Task<Result<TrabajoRealizadoDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var trabajo = await _repository.GetByIdAsync(id, cancellationToken);
        if (trabajo is null)
        {
            return Error.NotFound($"El trabajo {id} no existe.");
        }

        var imagenes = await LoadImagesAsync(trabajo.Id, cancellationToken);
        return TrabajoRealizadoDto.From(trabajo, imagenes);
    }

    public async Task<Result<TrabajoRealizadoDto>> CreateAsync(
        CreateTrabajoRealizadoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        try
        {
            var trabajo = TrabajoRealizado.Crear(
                dto.Nombre,
                dto.TipoMueble,
                dto.ClienteNombre,
                dto.PublicarNombreCliente,
                dto.Fecha,
                dto.Descripcion,
                dto.Medidas,
                dto.Melamina,
                dto.Accesorios,
                dto.Categoria,
                dto.Tags,
                dto.Destacado,
                dto.Publicado);

            await _repository.AddAsync(trabajo, cancellationToken);

            return TrabajoRealizadoDto.From(trabajo, []);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }
    }

    public async Task<Result<TrabajoRealizadoDto>> UpdateAsync(
        int id,
        UpdateTrabajoRealizadoDto dto,
        CancellationToken cancellationToken = default)
    {
        var trabajo = await _repository.GetByIdAsync(id, cancellationToken);
        if (trabajo is null)
        {
            return Error.NotFound($"El trabajo {id} no existe.");
        }

        try
        {
            trabajo.Actualizar(
                dto.Nombre,
                dto.TipoMueble,
                dto.ClienteNombre,
                dto.PublicarNombreCliente,
                dto.Fecha,
                dto.LimpiarFecha,
                dto.Descripcion,
                dto.Medidas,
                dto.Melamina,
                dto.Accesorios,
                dto.Categoria,
                dto.Tags);

            if (dto.Destacado.HasValue)
            {
                trabajo.MarcarDestacado(dto.Destacado.Value);
            }

            if (dto.Publicado.HasValue)
            {
                if (dto.Publicado.Value)
                {
                    trabajo.Publicar();
                }
                else
                {
                    trabajo.Ocultar();
                }
            }

            await _repository.UpdateAsync(trabajo, cancellationToken);

            var imagenes = await LoadImagesAsync(trabajo.Id, cancellationToken);
            return TrabajoRealizadoDto.From(trabajo, imagenes);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }
    }

    /// <summary>
    /// Borrado físico del trabajo y de sus referencias de imagen. Los
    /// objetos del storage se eliminan de forma best-effort (BR-161:
    /// la vidriera no es historial crítico).
    /// </summary>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var trabajo = await _repository.GetByIdAsync(id, cancellationToken);
        if (trabajo is null)
        {
            return Result.Failure(Error.NotFound($"El trabajo {id} no existe."));
        }

        var imagenes = await _imagenes.GetByTrabajoAsync(id, cancellationToken);

        await _repository.DeleteAsync(trabajo, cancellationToken);
        await _imagenes.DeleteByTrabajoAsync(id, cancellationToken);

        foreach (var imagen in imagenes)
        {
            try
            {
                await _storage.DeleteAsync(imagen.Path, CancellationToken.None);
            }
            catch
            {
                // Best-effort: la referencia ya no existe en la base.
            }
        }

        return Result.Success();
    }

    // ------------------------------------------------------------------
    // Vidriera pública (AGENTS.md §23)
    // ------------------------------------------------------------------

    public async Task<Result<PagedResult<ShowroomItemDto>>> GetShowroomPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? categoria,
        string? tipoMueble,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
        tipoMueble = string.IsNullOrWhiteSpace(tipoMueble) ? null : tipoMueble.Trim();

        var paged = await _repository.GetShowroomPagedAsync(
            page, pageSize, search, categoria, tipoMueble, cancellationToken);

        var portadas = await _imagenes.GetByTrabajoIdsAsync(
            paged.Items.Select(t => t.Id).ToList(), cancellationToken);

        var portadasPorTrabajo = portadas
            .GroupBy(i => i.TrabajoRealizadoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Orden).First());

        return new PagedResult<ShowroomItemDto>
        {
            Items = paged.Items.Select(t => ShowroomItemDto.From(
                t,
                portadasPorTrabajo.TryGetValue(t.Id, out var portada)
                    ? _storage.GetPublicUrl(portada.Path)
                    : null)).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    public async Task<Result<ShowroomTrabajoDto>> GetShowroomByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var trabajo = await _repository.GetPublishedByIdAsync(id, cancellationToken);
        if (trabajo is null)
        {
            return Error.NotFound($"El trabajo {id} no existe.");
        }

        var imagenes = await LoadImagesAsync(trabajo.Id, cancellationToken);
        return ShowroomTrabajoDto.From(trabajo, imagenes);
    }

    // ------------------------------------------------------------------
    // Imágenes (patrón de productos, AGENTS.md §24 / BR-101)
    // ------------------------------------------------------------------

    public async Task<Result<IReadOnlyList<TrabajoRealizadoImagenDto>>> GetImagesAsync(
        int trabajoRealizadoId,
        CancellationToken cancellationToken = default)
    {
        var trabajo = await _repository.GetByIdAsync(trabajoRealizadoId, cancellationToken);
        if (trabajo is null)
        {
            return Error.NotFound($"El trabajo {trabajoRealizadoId} no existe.");
        }

        var imagenes = await LoadImagesAsync(trabajoRealizadoId, cancellationToken);
        return Result.Success(imagenes);
    }

    /// <summary>
    /// Sube una imagen al storage y registra su referencia (BR-101).
    /// Valida extensión, MIME type y tamaño (AGENTS.md §24); si la
    /// persistencia falla se elimina el objeto huérfano.
    /// </summary>
    public async Task<Result<TrabajoRealizadoImagenDto>> AddImageAsync(
        int trabajoRealizadoId,
        string? fileName,
        string? contentType,
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        var trabajo = await _repository.GetByIdAsync(trabajoRealizadoId, cancellationToken);
        if (trabajo is null)
        {
            return Error.NotFound($"El trabajo {trabajoRealizadoId} no existe.");
        }

        if (content is null || length <= 0)
        {
            return Error.Validation("Debés adjuntar un archivo de imagen.");
        }

        if (length > MaxImageBytes)
        {
            return Error.Validation("La imagen no puede superar 5 MB.");
        }

        var extension = (Path.GetExtension(fileName ?? string.Empty) ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            return Error.Validation("Formato no permitido. Usá JPG, PNG, WEBP o GIF.");
        }

        var normalizedContentType = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedImageContentTypes.Contains(normalizedContentType))
        {
            return Error.Validation("El tipo de contenido de la imagen no es válido.");
        }

        if (!_storage.IsConfigured)
        {
            return Error.Storage(
                "El almacenamiento de imágenes no está configurado " +
                "(SUPABASE_URL / SUPABASE_STORAGE_BUCKET / SUPABASE_SERVICE_ROLE_KEY).");
        }

        var path = $"trabajos/{trabajoRealizadoId}/{Guid.NewGuid():N}{extension}";
        var upload = await _storage.UploadAsync(path, content, normalizedContentType, cancellationToken);
        if (upload.IsFailure)
        {
            return upload.Error;
        }

        try
        {
            var orden = await _imagenes.CountByTrabajoAsync(trabajoRealizadoId, cancellationToken);
            var imagen = TrabajoRealizadoImagen.Crear(trabajoRealizadoId, path, orden);
            await _imagenes.AddAsync(imagen, cancellationToken);

            return TrabajoRealizadoImagenDto.From(imagen, _storage.GetPublicUrl(path));
        }
        catch
        {
            // Limpieza de archivo huérfano si falla la persistencia (AGENTS.md §24).
            await _storage.DeleteAsync(path, CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Elimina la imagen del storage y su referencia. Una imagen de otro
    /// trabajo responde 404 (no se filtra su existencia).
    /// </summary>
    public async Task<Result> DeleteImageAsync(
        int trabajoRealizadoId,
        int imageId,
        CancellationToken cancellationToken = default)
    {
        var imagen = await _imagenes.GetByIdAsync(imageId, cancellationToken);
        if (imagen is null || imagen.TrabajoRealizadoId != trabajoRealizadoId)
        {
            return Result.Failure(Error.NotFound($"La imagen {imageId} no existe."));
        }

        var delete = await _storage.DeleteAsync(imagen.Path, cancellationToken);
        if (delete.IsFailure)
        {
            return Result.Failure(delete.Error);
        }

        await _imagenes.DeleteAsync(imagen, cancellationToken);

        return Result.Success();
    }

    private async Task<IReadOnlyList<TrabajoRealizadoImagenDto>> LoadImagesAsync(
        int trabajoRealizadoId,
        CancellationToken cancellationToken)
    {
        var imagenes = await _imagenes.GetByTrabajoAsync(trabajoRealizadoId, cancellationToken);

        return imagenes
            .OrderBy(i => i.Orden)
            .Select(i => TrabajoRealizadoImagenDto.From(i, _storage.GetPublicUrl(i.Path)))
            .ToList();
    }
}
