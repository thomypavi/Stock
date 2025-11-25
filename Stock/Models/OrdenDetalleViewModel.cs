using System.Collections.Generic;

namespace Stock.Models
{
    public class OrdenItem
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
    }

    public class OrdenDetalleViewModel
    {
        public int IdOrden { get; set; }
        public System.DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public int IdProveedor { get; set; }

        public List<OrdenItem> Items { get; set; }

        public double Total { get; set; }
    }
}