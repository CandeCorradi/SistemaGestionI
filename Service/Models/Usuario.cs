using Service.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } 
        public string Email { get; set; }
        public string Password { get; set; } 
        public TipoUsuarioEnum TipoUsuario { get; set; }  // Administrador, Cliente
        public int ClienteId { get; set; } // Relación con tabla Cliente
        public bool IsDeleted { get; set; } = false;

        public override string ToString()
        {
            return Nombre;
        }
    }
}
