namespace Stock.Models
{
    public class PedidoRequest
    {
        public int IdProducto { get; set; }
        public bool Seleccionado { get; set; }
        public int CantidadSolicitada { get; set; }
    }
}
