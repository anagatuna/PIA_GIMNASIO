using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaPaseLista
    {
        // Estatus de asistencia que maneja el pase de lista
        public const int Pendiente = 5;     // Reserva confirmada, sin asistencia registrada todavía
        public const int Asistio = 6;
        public const int Falta = 7;

        private readonly bdConexion _bd;

        public logicaPaseLista(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: clases del día 
        public DataTable ConsultarClases(DateTime fecha, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@Fecha", fecha.Date),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spPaseLista", p);
        }

        // Accion 2: socios con reserva en la clase
        public DataTable ConsultarParticipantes(int idClase, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdClase", idClase),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spPaseLista", p);
        }

        // Guardar asistencia: recibe el estatus de cada reserva (5, 6 o 7).
        // Primero se validan todos los socios; si alguno tiene error no se guarda nada.
        public bdRespuesta Guardar(int idClase, int? idEmpleado, Dictionary<int, int> asistencia, int usuarioSesion)
        {
            // Validar a todos (no guarda)
            string errores = "";
            foreach (KeyValuePair<int, int> a in asistencia)
            {
                bdRespuesta r = GuardarSocio(idClase, idEmpleado, a.Key, a.Value, true, usuarioSesion);
                if (r.TipoMensaje != "success" && !errores.Contains(r.Mensaje))
                    errores = errores + r.Mensaje + " ";
            }

            if (errores != "")
                return new bdRespuesta { Mensaje = errores.Trim(), TipoMensaje = "warning" };

            // Guardar a todos
            int guardados = 0;
            foreach (KeyValuePair<int, int> a in asistencia)
            {
                bdRespuesta r = GuardarSocio(idClase, idEmpleado, a.Key, a.Value, false, usuarioSesion);
                if (r.TipoMensaje == "success")
                    guardados++;
            }

            return new bdRespuesta
            {
                Mensaje = "Pase de lista guardado (" + guardados + " participantes).",
                TipoMensaje = "success"
            };
        }

        // Accion 3: valida o guarda la asistencia de un socio
        private bdRespuesta GuardarSocio(int idClase, int? idEmpleado, int idReserva, int idEstatus, bool soloValidar, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@IdClase", idClase),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado)),
                new SqlParameter("@IdReserva", idReserva),
                new SqlParameter("@IdEstatus", idEstatus),
                new SqlParameter("@SoloValidar", soloValidar),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spPaseLista", p);

            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }
    }
}
