using System.ComponentModel.DataAnnotations;

namespace PIA_Gimnasio.Entidades
{
    // Representa un usuario del sistema (tabla USUARIO)
    public class bdUsuario
    {
        // 0 = nuevo registro, > 0 = edición
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "Ingresa la cuenta.")]
        [StringLength(50, ErrorMessage = "La cuenta no puede pasar de 50 caracteres.")]
        [RegularExpression(@"^\S+$", ErrorMessage = "La cuenta no puede llevar espacios.")]
        public string? Cuenta { get; set; }

        // Obligatoria solo al registrar; al editar, vacía = conservar la actual
        [StringLength(50, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 50 caracteres.")]
        [DataType(DataType.Password)]
        public string? Contrasenia { get; set; }

        [Required(ErrorMessage = "Selecciona el rol.")]
        public int? IdRol { get; set; }

        [Required(ErrorMessage = "Selecciona el estatus.")]
        public int? IdEstatus { get; set; } = 1;
    }
}
