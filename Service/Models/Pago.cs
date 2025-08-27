using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Pago
    {
        public int Id { get; set; }
        //id del pedido
        public int PedidoId { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Monto { get; set; }
        public string TipoPago {get; set; }

        //agregamos delete
        public bool IsDeleted { get; set; } = false;
        public override string ToString()
        {
            return $"{Monto:C} - {FechaPago.ToShortDateString()} " +
                   $"- Pedido ID: {PedidoId}" +
                   $"- Monto: {Monto}" +
                   $"- Tipo de pago: {TipoPago}";
        }
    }
}
