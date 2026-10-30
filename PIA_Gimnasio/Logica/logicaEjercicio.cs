using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaEjercicio
    {
        private readonly bdConexion _bd;

        public logicaEjercicio(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1 (registrar) o 4 (modificar)
        public bdRespuesta Guardar(bdEjercicio ejercicio, int usuarioSesion)
        {
            int accion = ejercicio.IdEjercicio == 0 ? 1 : 4;

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdEjercicio", ejercicio.IdEjercicio),
                new SqlParameter("@IdEstatus", Valor(ejercicio.IdEstatus)),
                new SqlParameter("@Nombre", Valor(ejercicio.Nombre?.Trim().ToUpper())),
                new SqlParameter("@GrupoMuscular", Valor(ejercicio.GrupoMuscular)),
                new SqlParameter("@NivelDificultad", Valor(ejercicio.NivelDificultad)),
                new SqlParameter("@ContraIndicacion", Valor(ejercicio.ContraIndicacion?.Trim())),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spEjercicio", p));
        }

        // Accion 2: leer un registro
        public bdEjercicio? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdEjercicio", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spEjercicio", p);

            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new bdEjercicio
            {
                IdEjercicio = Convert.ToInt32(r["IdEjercicio"]),
                IdEstatus = Convert.ToInt32(r["IdEstatus"]),
                Nombre = r["Nombre"].ToString(),
                GrupoMuscular = r["GrupoMuscular"].ToString(),
                NivelDificultad = r["NivelDificultad"].ToString(),
                ContraIndicacion = r["ContraIndicacion"].ToString()
            };
        }

        // Accion 3: consulta general (filtros opcionales)
        public DataTable Consultar(string? nombre, string? grupoMuscular, string? nivelDificultad, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Nombre", Valor(nombre?.Trim())),
                new SqlParameter("@GrupoMuscular", Valor(grupoMuscular)),
                new SqlParameter("@NivelDificultad", Valor(nivelDificultad)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spEjercicio", p);
        }

        // Accion 5: eliminar (baja lógica)
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdEjercicio", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spEjercicio", p));
        }

        // Accion 6: ejercicios activos (para el diseñador de rutinas)
        public DataTable ConsultarActivos()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spEjercicio", p);
        }

        // Accion 7: combo de estatus
        public DataTable ConsultarEstatus()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 7) };
            return _bd.EjecutarStoredProcedure("spEjercicio", p);
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

        // Manda NULL a SQL cuando el número no tiene valor
        private object Valor(int? numero)
        {
            return numero.HasValue ? numero.Value : DBNull.Value;
        }
    }
}
