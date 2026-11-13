using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    public class LoginModel : PageModel
    {
        private readonly logicaUsuario _logica;

        public LoginModel(logicaUsuario logica)
        {
            _logica = logica;
        }

        // Datos del formulario
        [BindProperty]
        public bdUsuario Usuario { get; set; } = new bdUsuario();

        public string Mensaje { get; set; } = "";

        // Al abrir el Login
        public IActionResult OnGet()
        {
            // Si ya inició sesión, lo manda directo al inicio
            if (HttpContext.Session.GetInt32("IdUsuario") != null)
                return RedirectToPage("/Inicio");

            return Page();
        }

        // Botón Entrar
        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Usuario.Cuenta) || string.IsNullOrEmpty(Usuario.Contrasenia))
            {
                Mensaje = "Ingresa tu cuenta y tu contraseña.";
                return Page();
            }

            try
            {
                DataRow? u = _logica.IniciarSesion(Usuario.Cuenta, Usuario.Contrasenia);

                if (u == null)
                {
                    Mensaje = "Cuenta o contraseña incorrectas, favor de verificar.";
                    return Page();
                }

                // IdEstatus 1 = Activo
                if (Convert.ToInt32(u["IdEstatus"]) != 1)
                {
                    Mensaje = "Tu usuario está " + u["Estatus"] + ". Contacta al administrador.";
                    return Page();
                }

                // Guardar los datos en la sesión
                HttpContext.Session.SetInt32("IdUsuario", Convert.ToInt32(u["IdUsuario"]));
                HttpContext.Session.SetInt32("IdRol", Convert.ToInt32(u["IdRol"]));
                HttpContext.Session.SetString("Rol", u["Rol"].ToString() ?? "");

                // El empleado puede venir vacío si la cuenta no tiene empleado ligado
                if (u["IdEmpleado"] != DBNull.Value)
                {
                    HttpContext.Session.SetInt32("IdEmpleado", Convert.ToInt32(u["IdEmpleado"]));
                    HttpContext.Session.SetInt32("IdSucursal", Convert.ToInt32(u["IdSucursal"]));
                    HttpContext.Session.SetString("Nombre", u["Empleado"] + " " + u["PrimerApellido"]);
                }
                else
                {
                    HttpContext.Session.SetString("Nombre", u["Cuenta"].ToString() ?? "");
                }

                return RedirectToPage("/Inicio");
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos. Intenta más tarde.";
                return Page();
            }
        }

        // Cerrar sesión (botón del menú)
        public IActionResult OnPostSalir()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}
