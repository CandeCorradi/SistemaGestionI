using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Turno
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public string Estado { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        //agregamos delete
        public bool IsDeleted { get; set; } = false;
        public override string ToString()
        {
            return $"{FechaHora.ToShortDateString()} " +
                   $"- {FechaHora.ToShortTimeString()} " +
                   $"- {Estado} " +
                   $"- Cliente ID: {ClienteId}";
        }
    }
}
