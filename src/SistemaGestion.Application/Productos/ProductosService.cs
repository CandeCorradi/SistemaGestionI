using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Productos.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Productos;

/// <summary>
/// Casos de uso de productos (functional-spec §8, roadmap §13.1): alta,
/// consulta, búsqueda con filtros, edición parcial y desactivación lógica
/// (BR-032). El SKU es la identidad de negocio única (BR-030). Cambiar el
/// precio actual no afecta presupuestos ni pedidos históricos (BR-031).
/// Incluye el catálogo público (functional-spec §9) y la gestión de
/// imágenes en el storage externo (BR-100/BR-101, AGENTS.md §24).
/// </summary>
public sealed class ProductosService
{
    /// <summary>
    /// Tamaño de página máximo permitido (AGENTS.md §30: sin páginas ilimitadas).
    /// </summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    /// <summary>Tamaño máximo de una imagen de producto (bytes).</summary>
    public const long MaxImageBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedImageExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private static readonly string[] AllowedImageContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private readonly IProductoRepository _repository;

    private readonly IProductoImagenRepository _imagenes;

    private readonly IStorageService _storage;

    public ProductosService(
        IProductoRepository repository,
        IProductoImagenRepository imagenes,
        IStorageService storage)
    {
        _repository = repository;
        _imagenes = imagenes;
        _storage = storage;
    }

    public async Task<Result<PagedResult<ProductoListDto>>> GetListAsync(
        int page,
        int pageSize,
        string? search,
        TipoProducto? tipo,
        string? categoria,
        bool? activo,
        bool? showroom,
        bool? lowStock,
        bool? isCustom = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();

        if (tipo.HasValue && !Enum.IsDefined(tipo.Value))
        {
            return Error.Validation($"El tipo {tipo.Value} no es un tipo de producto válido.");
        }

        var paged = await _repository.GetPagedAsync(
            page, pageSize, search, tipo, categoria, activo, showroom, lowStock, isCustom, cancellationToken);

        return new PagedResult<ProductoListDto>
        {
            Items = paged.Items.Select(ProductoListDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    public async Task<Result<ProductoDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = await _repository.GetByIdAsync(id, cancellationToken);
        if (producto is null)
        {
            return Error.NotFound($"El producto {id} no existe.");
        }

        return ProductoDto.From(producto);
    }

    public async Task<Result<ProductoDto>> CreateAsync(
        CreateProductoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.CodigoSku))
        {
            return Error.Validation("El código SKU es obligatorio.");
        }

        if (!Enum.IsDefined(dto.Tipo))
        {
            return Error.Validation($"El tipo {dto.Tipo} no es un tipo de producto válido.");
        }

        var normalizedSku = dto.CodigoSku.Trim().ToUpperInvariant();
        if (await _repository.SkuExistsAsync(normalizedSku, excludeId: null, cancellationToken))
        {
            return Error.Conflict($"Ya existe un producto con el SKU {normalizedSku}.");
        }

        Producto producto;
        try
        {
            producto = Producto.Crear(
                dto.Nombre,
                dto.CodigoSku,
                dto.Tipo,
                dto.Precio,
                dto.Descripcion,
                dto.Categoria,
                dto.Costo,
                dto.Material,
                dto.Acabado,
                dto.Color,
                dto.Dimensiones,
                dto.StockActual,
                dto.StockMinimo,
                dto.Showroom,
                dto.ProveedorId,
                dto.EsPersonalizado);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.AddAsync(producto, cancellationToken);

        return ProductoDto.From(producto);
    }

    public async Task<Result<ProductoDto>> UpdateAsync(
        int id,
        UpdateProductoDto dto,
        CancellationToken cancellationToken = default)
    {
        var producto = await _repository.GetByIdAsync(id, cancellationToken);
        if (producto is null)
        {
            return Error.NotFound($"El producto {id} no existe.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre ?? producto.Nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.CodigoSku ?? producto.CodigoSku))
        {
            return Error.Validation("El código SKU es obligatorio.");
        }

        if (dto.Tipo.HasValue && !Enum.IsDefined(dto.Tipo.Value))
        {
            return Error.Validation($"El tipo {dto.Tipo.Value} no es un tipo de producto válido.");
        }

        if (!string.IsNullOrWhiteSpace(dto.CodigoSku))
        {
            var normalizedSku = dto.CodigoSku.Trim().ToUpperInvariant();
            if (await _repository.SkuExistsAsync(normalizedSku, id, cancellationToken))
            {
                return Error.Conflict($"Ya existe un producto con el SKU {normalizedSku}.");
            }
        }

        try
        {
            producto.Actualizar(
                dto.Nombre,
                dto.CodigoSku,
                dto.Tipo,
                dto.Precio,
                dto.Descripcion,
                dto.Categoria,
                dto.Costo,
                dto.Material,
                dto.Acabado,
                dto.Color,
                dto.Dimensiones,
                dto.StockMinimo,
                dto.Showroom,
                dto.ProveedorId,
                dto.EsPersonalizado,
                dto.Activo);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.UpdateAsync(producto, cancellationToken);

        return ProductoDto.From(producto);
    }

    /// <summary>
    /// Desactivación lógica (BR-032): los productos usados en
    /// presupuestos, pedidos o ventas históricas nunca se borran.
    /// Idempotente.
    /// </summary>
    public async Task<Result<ProductoDto>> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var producto = await _repository.GetByIdAsync(id, cancellationToken);
        if (producto is null)
        {
            return Error.NotFound($"El producto {id} no existe.");
        }

        producto.Desactivar();
        await _repository.UpdateAsync(producto, cancellationToken);

        return ProductoDto.From(producto);
    }

    // ------------------------------------------------------------------
    // Catálogo público (functional-spec §9, roadmap §13.4)
    // ------------------------------------------------------------------

    /// <summary>
    /// Listado público del catálogo: sólo productos activos y con datos
    /// de exhibición (sin costo, stock numérico, proveedor ni estado
    /// interno). La disponibilidad se expone como booleano.
    /// </summary>
    public async Task<Result<PagedResult<CatalogoItemDto>>> GetCatalogPagedAsync(
        int page,
        int pageSize,
        string? search,
        TipoProducto? tipo,
        string? categoria,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();

        if (tipo.HasValue && !Enum.IsDefined(tipo.Value))
        {
            return Error.Validation($"El tipo {tipo.Value} no es un tipo de producto válido.");
        }

        var paged = await _repository.GetPagedAsync(
            page, pageSize, search, tipo, categoria,
            activo: true, showroom: null, lowStock: null, isCustom: null, cancellationToken);

        var ids = paged.Items.Select(p => p.Id).ToList();
        var imagenes = await _imagenes.GetByProductoIdsAsync(ids, cancellationToken);
        var portadas = imagenes
            .GroupBy(i => i.ProductoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Orden).First());

        return new PagedResult<CatalogoItemDto>
        {
            Items = paged.Items.Select(p => new CatalogoItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Tipo = p.Tipo,
                Categoria = p.Categoria,
                Precio = p.Precio,
                EsPersonalizado = p.EsPersonalizado,
                Showroom = p.Showroom,
                Disponible = p.StockActual > 0,
                ImagenUrl = portadas.TryGetValue(p.Id, out var portada)
                    ? _storage.GetPublicUrl(portada.Path)
                    : null,
            }).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    /// <summary>
    /// Detalle público de un producto del catálogo. Los productos
    /// inactivos se comportan como inexistentes (404) para no filtrar su
    /// existencia (BR-004 aplicado al catálogo).
    /// </summary>
    public async Task<Result<CatalogoProductoDto>> GetCatalogByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var producto = await _repository.GetByIdAsync(id, cancellationToken);
        if (producto is null || !producto.Activo)
        {
            return Error.NotFound($"El producto {id} no existe o no está disponible.");
        }

        var imagenes = await _imagenes.GetByProductoAsync(id, cancellationToken);

        return new CatalogoProductoDto
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Tipo = producto.Tipo,
            Categoria = producto.Categoria,
            Precio = producto.Precio,
            Material = producto.Material,
            Acabado = producto.Acabado,
            Color = producto.Color,
            Dimensiones = producto.Dimensiones,
            EsPersonalizado = producto.EsPersonalizado,
            Showroom = producto.Showroom,
            Disponible = producto.StockActual > 0,
            Imagenes = imagenes
                .OrderBy(i => i.Orden)
                .Select(i => ProductoImagenDto.From(i, _storage.GetPublicUrl(i.Path)))
                .ToList(),
        };
    }

    // ------------------------------------------------------------------
    // Imágenes de producto (AGENTS.md §24, BR-100/BR-101)
    // ------------------------------------------------------------------

    /// <summary>Galería del producto (URLs públicas, orden ascendente).</summary>
    public async Task<Result<IReadOnlyList<ProductoImagenDto>>> GetImagesAsync(
        int productoId,
        CancellationToken cancellationToken = default)
    {
        var producto = await _repository.GetByIdAsync(productoId, cancellationToken);
        if (producto is null)
        {
            return Error.NotFound($"El producto {productoId} no existe.");
        }

        var imagenes = await _imagenes.GetByProductoAsync(productoId, cancellationToken);

        return imagenes
            .OrderBy(i => i.Orden)
            .Select(i => ProductoImagenDto.From(i, _storage.GetPublicUrl(i.Path)))
            .ToList();
    }

    /// <summary>
    /// Sube una imagen al storage y registra su referencia (BR-101).
    /// Valida extensión, MIME type y tamaño (AGENTS.md §24); si la
    /// persistencia falla se elimina el objeto huérfano.
    /// </summary>
    public async Task<Result<ProductoImagenDto>> AddImageAsync(
        int productoId,
        string? fileName,
        string? contentType,
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        var producto = await _repository.GetByIdAsync(productoId, cancellationToken);
        if (producto is null)
        {
            return Error.NotFound($"El producto {productoId} no existe.");
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

        var path = $"productos/{productoId}/{Guid.NewGuid():N}{extension}";
        var upload = await _storage.UploadAsync(path, content, normalizedContentType, cancellationToken);
        if (upload.IsFailure)
        {
            return upload.Error;
        }

        try
        {
            var orden = await _imagenes.CountByProductoAsync(productoId, cancellationToken);
            var imagen = ProductoImagen.Crear(productoId, path, orden);
            await _imagenes.AddAsync(imagen, cancellationToken);

            return ProductoImagenDto.From(imagen, _storage.GetPublicUrl(path));
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
    /// producto responde 404 (no se filtra su existencia).
    /// </summary>
    public async Task<Result> DeleteImageAsync(
        int productoId,
        int imageId,
        CancellationToken cancellationToken = default)
    {
        var imagen = await _imagenes.GetByIdAsync(imageId, cancellationToken);
        if (imagen is null || imagen.ProductoId != productoId)
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
}
