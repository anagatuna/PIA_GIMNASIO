using System.Text.RegularExpressions;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica.Validacion
{
    // Reglas de captura de usuarios. Regresa la lista de mensajes; si está vacía, los datos son válidos.
    // (La cuenta única y el empleado disponible se revisan contra la BD con logicaUsuario.)
    public static class validacionUsuario
    {
        public static List<string> Validar(bdUsuario usuario, int idUsuarioSesion)
        {
            List<string> errores = new List<string>();
            bool esNuevo = usuario.IdUsuario == 0;

            // Empleado (relación 1:1): obligatorio solo al registrar
            if (esNuevo && !usuario.IdEmpleado.HasValue)
                errores.Add("Selecciona el empleado al que pertenece la cuenta.");

            // Cuenta
            if (string.IsNullOrWhiteSpace(usuario.Cuenta))
                errores.Add("Ingresa la cuenta.");
            else if (usuario.Cuenta.Trim().Length > 50)
                errores.Add("La cuenta no puede pasar de 50 caracteres.");
            else if (Regex.IsMatch(usuario.Cuenta.Trim(), @"\s"))
                errores.Add("La cuenta no puede llevar espacios.");

            // Contraseña: obligatoria al registrar; al editar, vacía = conservar la actual
            if (esNuevo && string.IsNullOrWhiteSpace(usuario.Contrasenia))
                errores.Add("Ingresa la contraseña.");
            else if (!string.IsNullOrEmpty(usuario.Contrasenia) && (usuario.Contrasenia.Length < 6 || usuario.Contrasenia.Length > 50))
                errores.Add("La contraseña debe tener entre 6 y 50 caracteres.");

            // Rol y estatus
            if (!usuario.IdRol.HasValue)
                errores.Add("Selecciona el rol.");
            if (!usuario.IdEstatus.HasValue)
                errores.Add("Selecciona el estatus.");

            // Evita que el administrador se quite el acceso a sí mismo
            if (!esNuevo && usuario.IdUsuario == idUsuarioSesion && (usuario.IdEstatus != 1 || usuario.IdRol != 1))
                errores.Add("No puedes cambiar tu propio rol ni desactivar tu propio usuario.");

            return errores;
        }

        // Inicio de sesión
        public static List<string> ValidarLogin(string? cuenta, string? contrasenia)
        {
            List<string> errores = new List<string>();

            if (string.IsNullOrWhiteSpace(cuenta))
                errores.Add("Ingresa tu cuenta.");
            else if (cuenta.Trim().Length > 50)
                errores.Add("La cuenta no puede pasar de 50 caracteres.");

            if (string.IsNullOrEmpty(contrasenia))
                errores.Add("Ingresa tu contraseña.");
            else if (contrasenia.Length > 50)
                errores.Add("La contraseña no puede pasar de 50 caracteres.");

            return errores;
        }

        // Cambio de contraseña desde el perfil
        public static List<string> ValidarCambioContrasenia(string? actual, string? nueva, string? confirmacion)
        {
            List<string> errores = new List<string>();

            if (string.IsNullOrEmpty(actual))
                errores.Add("Ingresa tu contraseña actual.");
            if (string.IsNullOrEmpty(nueva))
                errores.Add("Ingresa la nueva contraseña.");
            else if (nueva.Length < 6 || nueva.Length > 50)
                errores.Add("La contraseña debe tener entre 6 y 50 caracteres.");
            else if (nueva == actual)
                errores.Add("La nueva contraseña debe ser diferente a la actual.");
            if (nueva != confirmacion)
                errores.Add("La confirmación no coincide con la nueva contraseña.");

            return errores;
        }
    }
}
