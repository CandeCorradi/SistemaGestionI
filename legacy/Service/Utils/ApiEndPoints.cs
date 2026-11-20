using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cliente.Utils
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
        public static string Login { get; set; } = "auth";

        public static string GetEndpoint(string typeName)
        {
            return typeName switch
            {
                nameof(Cliente) => Cliente,
                nameof(global::Cliente.Models.DetallePedido) => DetallePedido,
                nameof(global::Cliente.Models.Mayorista) => Mayorista,
                nameof(global::Cliente.Models.DetallePresupuesto) => DetallePresupuesto,
                nameof(global::Cliente.Models.Producto) => Producto,
                nameof(global::Cliente.Models.Pedido) => Pedido,
                nameof(global::Cliente.Models.Usuario) => Usuario,
                nameof(global::Cliente.Models.Pago) => Pago,
                nameof(global::Cliente.Models.Presupuesto) => Presupuesto,
                nameof(global::Cliente.Models.ProductoPresupuesto) => ProductoPresupuesto,
                nameof(global::Cliente.Models.Login) => Login,
                _ => throw new ArgumentException($"No endpoint defined for type {typeName}")
            };
        }
    }
}
