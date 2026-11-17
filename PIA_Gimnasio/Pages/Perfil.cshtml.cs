using System.Data;
using Microsoft.AspNetCore.Mvc;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Cualquier usuario con sesión puede ver su perfil y cambiar su contraseña
    public class PerfilModel : PaginaProtegida
    {
        private readonly logicaUsuario _logica;

        public PerfilModel(logicaUsuario logica)
        {
            _logica = logica;
        }

        // Formulario de cambio de contraseña
        [BindProperty]
        public bdCambioContrasenia Cambio { get; set; } = new bdCambioContrasenia();

        // Datos para la vista
        public DataRow? Perfil { get; set; }

        public void OnGet()
        {
            Cargar();
        }

        // Botón Cambiar contraseña
        public void OnPost()
        {
            if (string.IsNullOrEmpty(Cambio.Actual))
                Mensaje = "Ingresa tu contraseña actual.";
            else if (string.IsNullOrEmpty(Cambio.Nueva))
                Mensaje = "Ingresa la nueva contraseña.";
            else if (Cambio.Nueva.Length < 6 || Cambio.Nueva.Length > 50)
                Mensaje = "La contraseña debe tener entre 6 y 50 caracteres.";
            else if (Cambio.Nueva == Cambio.Actual)
                Mensaje = "La nueva contraseña debe ser diferente a la actual.";
            else if (Cambio.Nueva != Cambio.Confirmacion)
                Mensaje = "La confirmación no coincide con la nueva contraseña.";

            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return;
            }

            try
            {
                bdRespuesta r = _logica.CambiarContrasenia(IdUsuarioSesion(), Cambio.Actual ?? "", Cambio.Nueva ?? "");
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                // Si salió bien se vacía el formulario
                if (r.TipoMensaje == "success")
                {
                    ModelState.Clear();
                    Cambio = new bdCambioContrasenia();
                }
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al cambiar la contraseña.";
                TipoMensaje = "danger";
            }
            Cargar();
        }

        private void Cargar()
        {
            try
            {
                Perfil = _logica.ObtenerPerfil(IdUsuarioSesion());
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
