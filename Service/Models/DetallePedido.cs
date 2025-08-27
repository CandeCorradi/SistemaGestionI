using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class DetallePedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        //agregamos delete
        public bool IsDeleted { get; set; } = false;
        public override string ToString()
        {
            return $"Detalle ID: {Id} " +
                   $"- Pedido ID: {PedidoId} " +
                   $"- Melamina ID: {ProductoId} " +
                   $"- Cantidad: {Cantidad}";
        }
    }
}
