namespace Stock.Models
{
    public class SolicitudPedido
    {
        public int Id { get; set; }
        public int IdProducto { get; set; }
        public int IdProveedor { get; set; }
        public int CantidadSolicitada { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}

