namespace Stock.Models
{
    public class ProductoProveedorViewModel
    {
        public int IdProveedor { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public double Precio { get; set; }


        public int StockActual { get; set; }


        public int StockMinimo { get; set; }


        public int CantidadReposicion { get; set; }


    }
}