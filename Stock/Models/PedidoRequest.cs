namespace Stock.Models
{
    public class PedidoRequest
    {
        public int IdProducto { get; set; }
        public bool Seleccionado { get; set; }  // MUY IMPORTANTE -> bool
        public int CantidadSolicitada { get; set; }
    }
}
