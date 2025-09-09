using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class DetallePresupuesto
    {
        [Key]
        public int IdDetalle { get; set; }
        public int PresupuestoId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public bool IsDeleted { get; set; } = false;
        public override string ToString()
        {
            return $"Detalle ID: {IdDetalle} " +
                   $"- Pedido ID: {PresupuestoId} " +
                   $"- Melamina ID: {ProductoId} " +
                   $"- Cantidad: {Cantidad}";
        }
    }
}
