using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Usuarios.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Usuarios;

/// <summary>
/// Casos de uso de usuarios internos (functional-spec §5).
/// Las validaciones críticas se ejecutan siempre aquí, no solo en el
/// frontend (AGENTS.md §31).
/// </summary>
public sealed class UsuariosService
{
    /// <summary>
    /// Tamaño de página máximo permitido (AGENTS.md §30: sin páginas ilimitadas).
    /// </summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    /// <summary>
    /// Longitud mínima de contraseña aceptada en el autorregistro
    /// (la política de Firebase Authentication).
    /// </summary>
    public const int MinPasswordLength = 6;

    /// <summary>
    /// Longitud máxima de contraseña en el autorregistro.
    /// </summary>
    public const int MaxPasswordLength = 128;

    private readonly IUsuarioRepository _repository;

    private readonly IRolRepository _rolRepository;

    private readonly IFirebaseUserService _firebaseUsers;

    private readonly IClienteRepository _clientes;

    public UsuariosService(
        IUsuarioRepository repository,
        IRolRepository rolRepository,
        IFirebaseUserService firebaseUsers,
        IClienteRepository clientes)
    {
        _repository = repository;
        _rolRepository = rolRepository;
        _firebaseUsers = firebaseUsers;
        _clientes = clientes;
    }

    public async Task<Result<PagedResult<UsuarioListDto>>> GetListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var paged = await _repository.GetPagedAsync(page, pageSize, search, cancellationToken);

        return new PagedResult<UsuarioListDto>
        {
            Items = paged.Items.Select(UsuarioListDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    public async Task<Result<UsuarioDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _repository.GetByIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound($"El usuario {id} no existe.");
        }

        return UsuarioDto.From(usuario);
    }

    /// <summary>
    /// Perfil del usuario interno vinculado al Firebase UID autenticado
    /// (roadmap §18 — Profile). 404 si el uid no tiene usuario interno
    /// activo, igual que los permisos efectivos.
    /// </summary>
    public async Task<Result<AuthProfileDto>> GetProfileAsync(
        string? firebaseUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Error.NotFound("No existe un usuario interno asociado a la identidad autenticada.");
        }

        var usuario = await _repository.GetByFirebaseUidAsync(firebaseUid.Trim(), cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound("No existe un usuario interno activo asociado a la identidad autenticada.");
        }

        var rol = await _rolRepository.GetByIdAsync(usuario.RolId, cancellationToken);

        return new AuthProfileDto
        {
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Rol = rol?.Nombre ?? string.Empty,
            Activo = usuario.Activo,
        };
    }

    public async Task<Result<UsuarioDto>> CreateAsync(
        CreateUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(dto.Email, dto.Nombre, dto.Apellido);
        if (validationError is not null)
        {
            return validationError;
        }

        if (dto.RolId <= 0 || await _rolRepository.GetByIdAsync(dto.RolId, cancellationToken) is null)
        {
            return Error.Validation($"El rol {dto.RolId} no existe.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _repository.EmailExistsAsync(email, excludeId: null, cancellationToken))
        {
            return Error.Conflict($"Ya existe un usuario con el email {email}.");
        }

        Usuario usuario;
        try
        {
            usuario = Usuario.Crear(dto.Email, dto.Nombre, dto.Apellido, dto.RolId, dto.FirebaseUid);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.AddAsync(usuario, cancellationToken);

        return UsuarioDto.From(usuario);
    }

    /// <summary>
    /// Autorregistro de cuenta (Portal Cliente): crea la cuenta en
    /// Firebase Authentication y el usuario interno con el rol Cliente
    /// (BR-001: roles fijos). Si la persistencia interna falla, se
    /// elimina la cuenta creada en Firebase (rollback).
    /// </summary>
    public async Task<Result<RegisterResultDto>> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return Error.Validation("El email es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Apellido))
        {
            return Error.Validation("El apellido es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < MinPasswordLength)
        {
            return Error.Validation($"La contraseña debe tener al menos {MinPasswordLength} caracteres.");
        }

        if (dto.Password.Length > MaxPasswordLength)
        {
            return Error.Validation($"La contraseña no puede superar los {MaxPasswordLength} caracteres.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _repository.EmailExistsAsync(email, excludeId: null, cancellationToken))
        {
            return Error.Conflict($"Ya existe una cuenta con el email {email}.");
        }

        var rol = (await _rolRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(r => r.Codigo == Rol.ClienteCodigo);
        if (rol is null)
        {
            throw new InvalidOperationException("El rol Cliente no está configurado.");
        }

        // Valida email/nombre/apellido en dominio ANTES de crear la
        // cuenta externa, para no dejar cuentas huérfanas en Firebase.
        Usuario usuario;
        try
        {
            usuario = Usuario.Crear(email, dto.Nombre, dto.Apellido, rol.Id);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        var firebaseResult = await _firebaseUsers.CreateAuthUserAsync(
            usuario.Email, dto.Password, usuario.Nombre, usuario.Apellido, cancellationToken);
        if (firebaseResult.IsFailure)
        {
            return firebaseResult.Error;
        }

        usuario.VincularFirebaseUid(firebaseResult.Value);

        try
        {
            await _repository.AddAsync(usuario, cancellationToken);
            await VincularClienteDelPortalAsync(usuario, cancellationToken);
        }
        catch
        {
            // Rollback best-effort: no dejar huérfana la cuenta de
            // Firebase. Si el rollback también falla, el error del
            // alta original queda registrado por el manejo global.
            try
            {
                await _firebaseUsers.DeleteAuthUserAsync(firebaseResult.Value, CancellationToken.None);
            }
            catch (Exception)
            {
                // Se ignora: la cuenta huérfana se eliminará manualmente.
            }

            throw;
        }

        return new RegisterResultDto(
            usuario.Id, usuario.Email, usuario.Nombre, usuario.Apellido, rol.Nombre);
    }

    /// <summary>
    /// Aprovisiona la cuenta interna de una identidad externa ya autenticada
    /// (por ejemplo Google). Es idempotente: si el Firebase UID ya está
    /// vinculado devuelve el perfil; si existe un usuario con el mismo email
    /// le vincula el UID (sin duplicar la cuenta); si no existe, crea el
    /// usuario interno con rol Cliente y su registro de cliente (BR-010).
    /// El backend nunca confía en el rol enviado por el cliente: el rol se
    /// asigna siempre en el servidor (AGENTS.md §8).
    /// </summary>
    public async Task<Result<AuthProfileDto>> ProvisionExternalAsync(
        string? firebaseUid,
        string? email,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Error.Validation("La identidad externa no tiene un UID válido.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation("La cuenta externa no informó un email.");
        }

        var uid = firebaseUid.Trim();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // 1) Ya existe el usuario vinculado a este UID: no hacer nada.
        var porUid = await _repository.GetByFirebaseUidAsync(uid, cancellationToken);
        if (porUid is not null)
        {
            return await BuildProfileAsync(porUid, cancellationToken);
        }

        // 2) Existe un usuario con el mismo email: vincular el UID externo.
        var porEmail = await _repository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (porEmail is not null)
        {
            if (!porEmail.Activo)
            {
                return Error.Validation(
                    "La cuenta está desactivada. Contactá al administrador del sistema.");
            }

            if (string.IsNullOrWhiteSpace(porEmail.FirebaseUid))
            {
                porEmail.VincularFirebaseUid(uid);
                await _repository.UpdateAsync(porEmail, cancellationToken);
            }
            else if (porEmail.FirebaseUid != uid)
            {
                // El email ya está vinculado a otra identidad: no se permite
                // apropiarse de la cuenta desde un proveedor externo.
                return Error.Conflict(
                    "El email ya está vinculado a otra cuenta de acceso. Ingresá con ese método.");
            }

            await VincularClienteDelPortalAsync(porEmail, cancellationToken);

            return await BuildProfileAsync(porEmail, cancellationToken);
        }

        // 3) Cuenta nueva: rol Cliente fijo, igual que el autorregistro.
        var rol = (await _rolRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(r => r.Codigo == Rol.ClienteCodigo);
        if (rol is null)
        {
            throw new InvalidOperationException("El rol Cliente no está configurado.");
        }

        var (nombre, apellido) = SplitDisplayName(displayName, normalizedEmail);

        Usuario usuario;
        try
        {
            usuario = Usuario.Crear(normalizedEmail, nombre, apellido, rol.Id, uid);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.AddAsync(usuario, cancellationToken);
        await VincularClienteDelPortalAsync(usuario, cancellationToken);

        return new AuthProfileDto
        {
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Rol = rol.Nombre,
            Activo = usuario.Activo,
        };
    }

    private async Task<AuthProfileDto> BuildProfileAsync(
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        var rol = await _rolRepository.GetByIdAsync(usuario.RolId, cancellationToken);

        return new AuthProfileDto
        {
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Rol = rol?.Nombre ?? string.Empty,
            Activo = usuario.Activo,
        };
    }

    /// <summary>
    /// Deriva nombre y apellido del nombre visible del proveedor externo.
    /// Si no hay nombre usa la parte local del email; si no hay apellido usa
    /// un valor explícito para cumplir la validación del dominio (el usuario
    /// puede completarlo luego desde su perfil).
    /// </summary>
    private static (string Nombre, string Apellido) SplitDisplayName(
        string? displayName,
        string email)
    {
        var name = displayName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            var local = email.Split('@')[0];
            name = string.IsNullOrWhiteSpace(local) ? "Cliente" : local;
        }

        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var nombre = parts.Length > 0 ? parts[0] : "Cliente";
        var apellido = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1].Trim()
            : "No informado";

        return (nombre, apellido);
    }

    /// <summary>
    /// Vincula la cuenta del portal con el registro de cliente (BR-010):
    /// si ya existe un cliente activo con el mismo email se le asigna el
    /// Firebase UID; si no, se crea el registro con los mismos datos.
    /// Se ejecuta después de persistir el usuario dentro del bloque de
    /// rollback: ante un fallo de BD se elimina la cuenta de Firebase
    /// (mejor esfuerzo) y la excepción queda registrada por el manejo
    /// global.
    /// </summary>
    private async Task VincularClienteDelPortalAsync(
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        var existente = await _clientes.GetByEmailAsync(usuario.Email, cancellationToken);
        if (existente is not null)
        {
            if (string.IsNullOrWhiteSpace(existente.FirebaseUid))
            {
                existente.VincularFirebaseUid(usuario.FirebaseUid!);
                await _clientes.UpdateAsync(existente, cancellationToken);
            }

            return;
        }

        var cliente = Cliente.Crear(
            usuario.Nombre,
            usuario.Apellido,
            usuario.Email,
            firebaseUid: usuario.FirebaseUid);
        await _clientes.AddAsync(cliente, cancellationToken);
    }

    public async Task<Result<UsuarioDto>> UpdateAsync(
        int id,
        UpdateUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repository.GetByIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return Error.NotFound($"El usuario {id} no existe.");
        }

        if (dto.RolId.HasValue)
        {
            if (dto.RolId.Value <= 0 || await _rolRepository.GetByIdAsync(dto.RolId.Value, cancellationToken) is null)
            {
                return Error.Validation($"El rol {dto.RolId.Value} no existe.");
            }
        }

        if (dto.Email is not null)
        {
            var validationError = Validate(dto.Email, dto.Nombre ?? usuario.Nombre, dto.Apellido ?? usuario.Apellido);
            if (validationError is not null)
            {
                return validationError;
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            if (!string.Equals(email, usuario.Email, StringComparison.Ordinal)
                && await _repository.EmailExistsAsync(email, id, cancellationToken))
            {
                return Error.Conflict($"Ya existe un usuario con el email {email}.");
            }
        }

        try
        {
            usuario.Actualizar(dto.Nombre, dto.Apellido, dto.Email, dto.Activo, dto.RolId);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        await _repository.UpdateAsync(usuario, cancellationToken);

        return UsuarioDto.From(usuario);
    }

    private static Error? Validate(string? email, string? nombre, string? apellido)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation("El email es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Error.Validation("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(apellido))
        {
            return Error.Validation("El apellido es obligatorio.");
        }

        return null;
    }
}
