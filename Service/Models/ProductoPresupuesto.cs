using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class ProductoPresupuesto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Medida { get; set; }
        public int Stock { get; set; }
        public string Categoria { get; set; } = string.Empty; // Melamina, Accesorio, Insumo
        public decimal Precio { get; set; }
        public bool IsDeleted { get; set; } = false;

        public override string ToString()
        {
            //que me devuelva nombre, descripcion, precio si hay stock, de lo contrario un message de sin stock
            return Stock > 0 
                ? $"{Nombre} - {Descripcion} - Precio: {Precio:C}" 
                : $"{Nombre} - Sin stock disponible";
        }
    }
}
