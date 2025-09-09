using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Utils
{
    public static class ApiEndPoints
    {
        public static string Cliente { get; set; } = "clientes";
        public static string DetallePedido { get; set; } = "detallepedidos";
        public static string Mayorista { get; set; } = "mayoristas";
        public static string DetallePresupuesto { get; set; } = "detallepresupuestos";
        public static string Producto { get; set; } = "productos";
        public static string Pedido { get; set; } = "pedidos";
        public static string Usuario { get; set; } = "usuarios";
        public static string Pago { get; set; } = "pagos";
        public static string Presupuesto { get; set; } = "presupuestos";
        public static string ProductoPresupuesto { get; set; } = "productopresupuestos";

        public static string GetEndpoint(string typeName)
        {
            return typeName switch
            {
                nameof(Service.Models.Cliente) => Cliente,
                nameof(Service.Models.DetallePedido) => DetallePedido,
                nameof(Service.Models.Mayorista) => Mayorista,
                nameof(Service.Models.DetallePresupuesto) => DetallePresupuesto,
                nameof(Service.Models.Producto) => Producto,
                nameof(Service.Models.Pedido) => Pedido,
                nameof(Service.Models.Usuario) => Usuario,
                nameof(Service.Models.Pago) => Pago,
                nameof(Service.Models.Presupuesto) => Presupuesto,
                nameof(Service.Models.ProductoPresupuesto) => ProductoPresupuesto,
                _ => throw new ArgumentException($"No endpoint defined for type {typeName}")
            };
        }
    }
}
