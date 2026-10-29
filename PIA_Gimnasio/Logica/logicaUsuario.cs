using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaUsuario
    {
        private readonly bdConexion _bd;

        public logicaUsuario(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: login (regresa el usuario con su rol, estatus y empleado, o null si no coincide)
        public DataRow? IniciarSesion(string cuenta, string contrasenia)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@Cuenta", cuenta.Trim()),
                new SqlParameter("@Contrasenia", contrasenia)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spUsuario", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 3: consulta general (filtra por cuenta, rol y estatus si se mandan)
        public DataTable Consultar(string? cuenta, int? idRol, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Cuenta", Valor(cuenta?.Trim())),
                new SqlParameter("@IdRol", Valor(idRol)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spUsuario", p);
        }

        // Accion 4: leer un registro (no trae la contraseña)
        public bdUsuario? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdUsuario", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spUsuario", p);

            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new bdUsuario
            {
                IdUsuario = Convert.ToInt32(r["IdUsuario"]),
                Cuenta = r["Cuenta"].ToString(),
                IdRol = Convert.ToInt32(r["IdRol"]),
                IdEstatus = Convert.ToInt32(r["IdEstatus"])
            };
        }

        // Accion 5 (registrar) o 6 (modificar)
        public bdRespuesta Guardar(bdUsuario usuario, int usuarioSesion)
        {
            int accion = usuario.IdUsuario == 0 ? 5 : 6;

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdUsuario", usuario.IdUsuario),
                new SqlParameter("@Cuenta", Valor(usuario.Cuenta?.Trim())),
                new SqlParameter("@Contrasenia", Valor(usuario.Contrasenia)),
                new SqlParameter("@IdRol", Valor(usuario.IdRol)),
                new SqlParameter("@IdEstatus", Valor(usuario.IdEstatus)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spUsuario", p));
        }

        // Accion 7: eliminar (baja lógica)
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 7),
                new SqlParameter("@IdUsuario", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spUsuario", p));
        }

        // Accion 8: combo de roles
        public DataTable ConsultarRoles()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 8) };
            return _bd.EjecutarStoredProcedure("spUsuario", p);
        }

        // Accion 9: combo de estatus (Activo / Inactivo)
        public DataTable ConsultarEstatus()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 9) };
            return _bd.EjecutarStoredProcedure("spUsuario", p);
        }

        // Convierte el Mensaje y TipoMensaje del SP en bdRespuesta
        private bdRespuesta Respuesta(DataTable dt)
        {
            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }

        // Manda NULL a SQL cuando el texto viene vacío
        private object Valor(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto;
        }

        // Manda NULL a SQL cuando el número no tiene valor (filtros y combos sin seleccionar)
        private object Valor(int? numero)
        {
            return numero.HasValue ? numero.Value : DBNull.Value;
        }
    }
}
