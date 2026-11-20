using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaGestion.Application.Clientes;
using SistemaGestion.Application.Clientes.Dtos;
using SistemaGestion.Application.Facturacion;
using SistemaGestion.Application.Facturacion.Dtos;
using SistemaGestion.Application.Pagos;
using SistemaGestion.Application.Pagos.Dtos;
using SistemaGestion.Application.Pedidos;
using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Application.Presupuestos;
using SistemaGestion.Application.Presupuestos.Dtos;
using SistemaGestion.Application.Productos;
using SistemaGestion.Application.Productos.Dtos;
using SistemaGestion.Application.Proveedores;
using SistemaGestion.Application.Proveedores.Dtos;
using SistemaGestion.Application.TrabajosRealizados;
using SistemaGestion.Application.TrabajosRealizados.Dtos;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Infrastructure.Persistence;

namespace SistemaGestion.API.Seeding;

/// <summary>
/// Datos ficticios de desarrollo (SOLO ambiente Development): catálogo,
/// clientes, presupuestos en varios estados, pedidos avanzados, pagos y
/// una factura simulada, para poder probar el panel sin cargar datos a
/// mano. Idempotente: si ya existen datos de negocio no hace nada.
/// No crea usuarios ni contraseñas: la identidad vive en Firebase
/// Authentication (AGENTS.md §3.5).
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>Firebase UID del SuperAdmin ya existente (fila interna Id 1).</summary>
    private const string SuperAdminUid = "W5d0odBAj6aohldtz3Sl0savB0g2";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var context = services.GetRequiredService<SistemaGestionDbContext>();

        if (await context.Clientes.AnyAsync()
            || await context.Productos.AnyAsync()
            || await context.Pedidos.AnyAsync())
        {
            logger.LogInformation("SEED_SKIP: ya existen datos de negocio en la base de desarrollo.");
            return;
        }

        logger.LogInformation("SEED_START: generando datos ficticios de desarrollo…");

        var clientes = services.GetRequiredService<ClientesService>();
        var proveedores = services.GetRequiredService<ProveedoresService>();
        var productos = services.GetRequiredService<ProductosService>();
        var presupuestos = services.GetRequiredService<PresupuestosService>();
        var pedidos = services.GetRequiredService<PedidosService>();
        var pagos = services.GetRequiredService<PagosService>();
        var facturas = services.GetRequiredService<FacturasService>();
        var trabajos = services.GetRequiredService<TrabajosRealizadosService>();

        // --- Clientes ---
        var laura = await RequireAsync(clientes.CreateAsync(new CreateClienteDto(
            "Laura", "Gómez", "laura.gomez@mail.test", "341-555-0101", "Av. Principal 123")), "cliente Laura");
        var martin = await RequireAsync(clientes.CreateAsync(new CreateClienteDto(
            "Martín", "Pérez", "martin.perez@mail.test", "341-555-0102", "Calle 5 456")), "cliente Martín");
        var carolina = await RequireAsync(clientes.CreateAsync(new CreateClienteDto(
            "Carolina", "Ruiz", "carolina.ruiz@mail.test", "341-555-0103", "Ruta 8 km 4")), "cliente Carolina");
        var diego = await RequireAsync(clientes.CreateAsync(new CreateClienteDto(
            "Diego", "Fernández", "diego.fernandez@mail.test", "341-555-0104", "Barrio Norte 78")), "cliente Diego");
        var sofia = await RequireAsync(clientes.CreateAsync(new CreateClienteDto(
            "Sofía", "Morales", "sofia.morales@mail.test", "341-555-0105", "Centro 90")), "cliente Sofía");

        // --- Proveedores ---
        var melaminas = await RequireAsync(proveedores.CreateAsync(new CreateProveedorDto(
            "Melamina del Sur", "Ventas", "ventas@melaminadelsur.test", "341-555-0201")), "proveedor Melamina del Sur");
        var herrajes = await RequireAsync(proveedores.CreateAsync(new CreateProveedorDto(
            "Herrajes Express", "Pedidos", "pedidos@herrajesexpress.test", "341-555-0202")), "proveedor Herrajes Express");
        var maderas = await RequireAsync(proveedores.CreateAsync(new CreateProveedorDto(
            "Maderas Norte", "Compras", "compras@maderasnorte.test", "341-555-0203")), "proveedor Maderas Norte");

        // --- Productos ---
        var escritorio = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Escritorio Ejecutivo 1800", "MUE-ESC-001", TipoProducto.Mueble, 1800m,
            Descripcion: "Escritorio de 1800x800 con dos cajones",
            Categoria: "Oficina", Material: "Melamina 18mm", StockMinimo: 0)),
            "producto escritorio");
        var comedor = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Comedor 6 Personas", "MUE-COM-002", TipoProducto.Mueble, 2500m,
            Descripcion: "Mesa comedor con 6 sillas coordinadas",
            Categoria: "Comedor", Material: "Madera y melamina", StockMinimo: 0)),
            "producto comedor");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Estantería Modular", "MUE-EST-003", TipoProducto.Mueble, 900m,
            Descripcion: "Estantería de 5 baldas modulares",
            Categoria: "Oficina", Material: "Melamina 18mm", StockMinimo: 0)),
            "producto estantería");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Melamina Blanca 9mm", "MEL-BLA-001", TipoProducto.Melamina, 45m,
            Categoria: "Melaminas", Material: "Melamina", ProveedorId: melaminas.Id,
            StockActual: 12, StockMinimo: 4)),
            "producto melamina blanca");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Melamina Nogal 18mm", "MEL-NOG-002", TipoProducto.Melamina, 68m,
            Categoria: "Melaminas", Material: "Melamina", ProveedorId: melaminas.Id,
            StockActual: 8, StockMinimo: 3)),
            "producto melamina nogal");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Manija Cromada", "ACC-MAN-001", TipoProducto.Accesorio, 12m,
            Categoria: "Accesorios", ProveedorId: herrajes.Id,
            StockActual: 40, StockMinimo: 10)),
            "producto manija");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Cierre Blum", "ACC-CIE-002", TipoProducto.Accesorio, 25m,
            Categoria: "Accesorios", ProveedorId: herrajes.Id,
            StockActual: 25, StockMinimo: 8)),
            "producto cierre");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Guía Telescópica", "ACC-GUI-003", TipoProducto.Accesorio, 30m,
            Categoria: "Accesorios", ProveedorId: herrajes.Id,
            StockActual: 30, StockMinimo: 8)),
            "producto guía");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Cola Blanca 1L", "INS-COL-001", TipoProducto.Insumo, 8m,
            Categoria: "Insumos", ProveedorId: maderas.Id,
            StockActual: 15, StockMinimo: 5)),
            "producto cola");
        _ = await RequireAsync(productos.CreateAsync(new CreateProductoDto(
            "Lija Grano Fino", "INS-LIJ-002", TipoProducto.Insumo, 3m,
            Categoria: "Insumos", ProveedorId: maderas.Id,
            StockActual: 50, StockMinimo: 20)),
            "producto lija");

        // --- Presupuesto 1: enviado y aceptado → pedido en producción ---
        var q1 = await RequireAsync(presupuestos.CreateAsync(new CreateQuoteDto(
            laura.Id, DateTime.UtcNow.AddDays(15), "Presupuesto de escritorio de oficina",
            "Entrega estimada: 20 días hábiles",
            [
                new CreateQuoteItemDto(escritorio.Id, null, 1, null, null),
                new CreateQuoteItemDto(null, "Instalación en domicilio", 1, 150m, null),
            ])), "presupuesto 1");
        await RequireAsync(presupuestos.SendAsync(q1.Id), "envío presupuesto 1");
        var q1Aceptado = await RequireAsync(presupuestos.AcceptAsync(q1.Id), "aceptación presupuesto 1");
        var pedido1 = await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, q1Aceptado.OrderId!.Value, new ChangeOrderStatusDto(EstadoPedido.Confirmado, "Confirmado (demo)")),
            "pedido 1 → Confirmado");
        await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, pedido1.Id, new ChangeOrderStatusDto(EstadoPedido.EnProduccion, "En producción (demo)")),
            "pedido 1 → EnProduccion");

        // --- Presupuesto 2: enviado, sin responder (queda Enviado) ---
        var q2 = await RequireAsync(presupuestos.CreateAsync(new CreateQuoteDto(
            martin.Id, DateTime.UtcNow.AddDays(10), "Comedor completo", null,
            [
                new CreateQuoteItemDto(comedor.Id, null, 1, null, null),
            ])), "presupuesto 2");
        await RequireAsync(presupuestos.SendAsync(q2.Id), "envío presupuesto 2");

        // --- Presupuesto 3: borrador ---
        _ = await RequireAsync(presupuestos.CreateAsync(new CreateQuoteDto(
            carolina.Id, DateTime.UtcNow.AddDays(20), "Estantería para estudio", null,
            [
                new CreateQuoteItemDto(null, "Estantería a medida 2000x2000", 1, 1400m, 50m),
            ])), "presupuesto 3");

        // --- Presupuesto 4: enviado y rechazado ---
        var q4 = await RequireAsync(presupuestos.CreateAsync(new CreateQuoteDto(
            diego.Id, DateTime.UtcNow.AddDays(8), "Escritorio + estantería", null,
            [
                new CreateQuoteItemDto(escritorio.Id, null, 1, null, null),
            ])), "presupuesto 4");
        await RequireAsync(presupuestos.SendAsync(q4.Id), "envío presupuesto 4");
        await RequireAsync(presupuestos.RejectAsync(q4.Id, "Fuera de presupuesto (demo)"), "rechazo presupuesto 4");

        // --- Presupuesto 5: aceptado → pedido listo + seña ---
        var q5 = await RequireAsync(presupuestos.CreateAsync(new CreateQuoteDto(
            sofia.Id, DateTime.UtcNow.AddDays(15), "Mueble de ropa a medida", null,
            [
                new CreateQuoteItemDto(null, "Mueble de ropa 2400 con 6 puertas", 1, 3200m, null),
            ])), "presupuesto 5");
        await RequireAsync(presupuestos.SendAsync(q5.Id), "envío presupuesto 5");
        var q5Aceptado = await RequireAsync(presupuestos.AcceptAsync(q5.Id), "aceptación presupuesto 5");
        var pedido2 = await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, q5Aceptado.OrderId!.Value, new ChangeOrderStatusDto(EstadoPedido.Confirmado, "Confirmado (demo)")),
            "pedido 2 → Confirmado");
        await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, pedido2.Id, new ChangeOrderStatusDto(EstadoPedido.EnProduccion, "En producción (demo)")),
            "pedido 2 → EnProduccion");
        pedido2 = await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, pedido2.Id, new ChangeOrderStatusDto(EstadoPedido.Listo, "Listo para entrega (demo)")),
            "pedido 2 → Listo");
        await RequireAsync(pagos.RegisterAsync(
            SuperAdminUid, pedido2.Id,
            new RegisterPaymentDto(500m, MetodoPago.Transferencia, "TR-DEMO-0001", "Seña (demo)")),
            "pago seña pedido 2");

        // --- Presupuesto 6: aceptado → pedido entregado + pago + factura simulada ---
        var q6 = await RequireAsync(presupuestos.CreateAsync(new CreateQuoteDto(
            laura.Id, DateTime.UtcNow.AddDays(12), "Escritorio para estudio", null,
            [
                new CreateQuoteItemDto(escritorio.Id, null, 1, null, null),
            ])), "presupuesto 6");
        await RequireAsync(presupuestos.SendAsync(q6.Id), "envío presupuesto 6");
        var q6Aceptado = await RequireAsync(presupuestos.AcceptAsync(q6.Id), "aceptación presupuesto 6");
        var pedido3 = await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, q6Aceptado.OrderId!.Value, new ChangeOrderStatusDto(EstadoPedido.Confirmado, "Confirmado (demo)")),
            "pedido 3 → Confirmado");
        await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, pedido3.Id, new ChangeOrderStatusDto(EstadoPedido.EnProduccion, "En producción (demo)")),
            "pedido 3 → EnProduccion");
        await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, pedido3.Id, new ChangeOrderStatusDto(EstadoPedido.Listo, "Listo para entrega (demo)")),
            "pedido 3 → Listo");
        pedido3 = await RequireAsync(pedidos.ChangeStatusAsync(
            SuperAdminUid, pedido3.Id, new ChangeOrderStatusDto(EstadoPedido.Entregado, "Entregado (demo)")),
            "pedido 3 → Entregado");
        await RequireAsync(pagos.RegisterAsync(
            SuperAdminUid, pedido3.Id,
            new RegisterPaymentDto(1000m, MetodoPago.Efectivo, null, "Pago parcial (demo)")),
            "pago pedido 3");
        await RequireAsync(facturas.CreateAsync(
            SuperAdminUid, new CreateFacturaDto(pedido3.Id, "Factura simulada de demostración")),
            "factura pedido 3");

        // --- Trabajos realizados (vidriera, AGENTS.md §23) ---
        _ = await RequireAsync(trabajos.CreateAsync(new CreateTrabajoRealizadoDto(
            "Cocina integral a medida",
            TipoMueble: "Cocina",
            ClienteNombre: "Laura Gómez",
            PublicarNombreCliente: true,
            Fecha: new DateOnly(2026, 3, 15),
            Descripcion: "Cocina completa con isla central y frentes lisos.",
            Medidas: "3200x2400x900",
            Melamina: "Blanca mate 18mm",
            Accesorios: "Bisagras Blum, manijas negras",
            Categoria: "Cocinas",
            Tags: ["cocina", "isla", "a-medida"],
            Destacado: true,
            Publicado: true)), "trabajo cocina");

        _ = await RequireAsync(trabajos.CreateAsync(new CreateTrabajoRealizadoDto(
            "Ropero de cuatro puertas",
            TipoMueble: "Ropero",
            ClienteNombre: "Martín Pérez",
            PublicarNombreCliente: false,
            Fecha: new DateOnly(2026, 5, 2),
            Descripcion: "Ropero empotrado con cajones interiores y espejo.",
            Medidas: "2400x2200x600",
            Melamina: "Nogal 18mm",
            Categoria: "Dormitorios",
            Tags: ["ropero", "empotrado"],
            Destacado: false,
            Publicado: true)), "trabajo ropero");

        _ = await RequireAsync(trabajos.CreateAsync(new CreateTrabajoRealizadoDto(
            "Biblioteca de living",
            TipoMueble: "Estantería",
            ClienteNombre: null,
            Fecha: new DateOnly(2026, 6, 20),
            Descripcion: "Biblioteca con módulos abiertos y cajones.",
            Categoria: "Living",
            Tags: ["biblioteca", "living"],
            Destacado: false,
            Publicado: false)), "trabajo biblioteca (borrador)");

        logger.LogInformation(
            "SEED_DONE: 5 clientes, 3 proveedores, 10 productos, 6 presupuestos, 3 pedidos, 2 pagos, 1 factura, 3 trabajos.");
    }

    private static async Task<T> RequireAsync<T>(
        Task<SistemaGestion.Shared.Results.Result<T>> task,
        string operacion)
    {
        var result = await task;
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"SEED_ERROR en '{operacion}': [{result.Error.Code}] {result.Error.Description}");
        }

        return result.Value;
    }
}
