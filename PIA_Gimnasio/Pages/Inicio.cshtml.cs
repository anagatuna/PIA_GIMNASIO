using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PIA_Gimnasio.Seguridad;

namespace PIA_Gimnasio.Pages
{
    // Pantalla de inicio: bienvenida al usuario según su rol
    public class InicioModel : PageModel
    {
        public string Nombre { get; set; } = "";
        public string Rol { get; set; } = "";

        public void OnGet()
        {
            Nombre = User.FindFirstValue("NombreCompleto") ?? User.Identity?.Name ?? "";
            Rol = User.FindFirstValue(ClaimTypes.Role) ?? "";
        }

        // Texto de bienvenida según el rol (matriz de permisos del PIA)
        public string DescripcionRol => Rol switch
        {
            Roles.Administrador => "Tienes acceso total al sistema, incluida la creación de cuentas de usuario.",
            Roles.Recepcionista => "Registra socios, controla el acceso de tu sucursal y captura reservas de clases.",
            Roles.Entrenador => "Consulta expedientes médicos, diseña y asigna rutinas y pasa lista en tus clases.",
            _ => ""
        };
    }
}
