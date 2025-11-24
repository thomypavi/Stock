using System.ComponentModel.DataAnnotations;

namespace Stock.Models
{
    public class Proveedor
    {
        [Key]
        public int IdProveedor { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty; 

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [Phone]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        public string Direccion { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        public string EmailContacto { get; set; } = string.Empty;
    }
}