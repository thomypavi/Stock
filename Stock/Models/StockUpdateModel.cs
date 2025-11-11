namespace Stock.Models
{
    public class StockUpdateModel
    {
        public int IdProducto { get; set; }        // 🔹 ID del producto en la tabla Productos
        public int CantidadActual { get; set; }    // 🔹 Cantidad actual mostrada en pantalla
        public int CantidadAgregar { get; set; }   // 🔹 Cantidad que el admin quiere sumar
    }
}

