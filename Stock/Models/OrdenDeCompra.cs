using System.ComponentModel.DataAnnotations;

namespace Stock.Models
{
    public class OrdenDeCompra
    {
        [Key]
        public int IdOrden { get; set; }

        public System.DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public int IdProveedor { get; set; }
    }
}
