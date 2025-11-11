namespace Stock.Models
{
    public class StockAdmin
    {
        public int Id { get; set; }
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public int Cantidad { get; set; }
    }
}

