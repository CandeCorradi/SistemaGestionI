using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Presupuesto
    {
        public int Id { get; set; } //relacion con tabla detallePresupuesto
        public int ClientId { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public DateTime FechaVencimiento { get; set; }  
        public decimal MontoEstimado { get; set; }
        public bool IsDeleted { get; set; } = false;

        public override string ToString()
        {
            return $"Presupuesto {Id} para cliente {ClientId}";
        }
    }
}
