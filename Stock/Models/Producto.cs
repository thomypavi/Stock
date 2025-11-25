using System.ComponentModel.DataAnnotations;

namespace Stock.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(8000)]
        public string Descripcion { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio es obligatorio y debe ser mayor que 0")]
        public decimal Precio { get; set; }

        public int IdProveedor { get; set; }
    }
}
