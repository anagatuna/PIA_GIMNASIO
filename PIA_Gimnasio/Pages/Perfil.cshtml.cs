using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Cualquier usuario con sesión puede ver su perfil
    public class PerfilModel : PaginaBase
    {
        private readonly logicaUsuario _logica;

        public PerfilModel(logicaUsuario logica)
        {
            _logica = logica;
        }

        // Formulario de cambio de contraseña
        [BindProperty]
        public CambioContrasenia Cambio { get; set; } = new();

        public class CambioContrasenia
        {
            public string? Actual { get; set; }
            public string? Nueva { get; set; }
            public string? Confirmacion { get; set; }
        }

        // Datos para la vista
        public DataRow? Perfil { get; set; }

        private void CargarPerfil()
        {
            try
            {
                Perfil = _logica.ObtenerPerfil(IdUsuarioSesion());
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public void OnGet()
        {
            CargarPerfil();
        }

        // Botón Cambiar contraseña
        public void OnPost()
        {
            // Reglas de captura (Logica/Validacion/validacionUsuario.cs)
            List<string> errores = validacionUsuario.ValidarCambioContrasenia(Cambio.Actual, Cambio.Nueva, Cambio.Confirmacion);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPerfil();
                return;
            }

            try
            {
                MostrarRespuesta(_logica.CambiarContrasenia(IdUsuarioSesion(), Cambio.Actual!, Cambio.Nueva!));

                // Si salió bien se vacía el formulario
                if (TipoMensaje == "success")
                {
                    ModelState.Clear();
                    Cambio = new CambioContrasenia();
                }
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al cambiar la contraseña.", "danger");
            }

            CargarPerfil();
        }
    }
}
