using System;
using System.ComponentModel.DataAnnotations;

namespace Service.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;

        //agregamos delete
        public bool IsDeleted { get; set; } = false;

        public override string ToString()
        {
            return $"{Nombre} {Apellido}";
        }
    }
}
