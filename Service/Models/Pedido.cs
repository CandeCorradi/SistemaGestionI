using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public  class Pedido
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Estado { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        //agregamos delete
        public bool IsDeleted { get; set; } = false;
        public override string ToString()
        {
            return $"{Fecha.ToShortDateString()} " +
                   $"- {Estado} " +
                   $"- Cliente ID: {ClienteId}";
        }
    }
}
