using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que 0")]
        public decimal Precio { get; set; }

        public int IdProveedor { get; set; }


        [Column("StockActual")]
        public int StockActual { get; set; } = 0; 

        [Column("StockMinimo")]
        public int StockMinimo { get; set; } = 0; 

        [Column("CantidadReposicion")]
        public int CantidadReposicion { get; set; } = 0; 

        public int Cantidad { get; set; }

        public int StockAdmin { get; set; }

    }
}