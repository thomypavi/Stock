namespace Stock.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty; // "Proveedor" o "Administrativo"
    }
}
