using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaSuscripcion
    {
        private readonly bdConexion _bd;

        public logicaSuscripcion(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: registrar
        public bdRespuesta Registrar(bdSuscripcion s, int usuarioSesion)
        {
            // Vigencia de un mes: del inicio al mismo día del mes siguiente menos un día
            DateTime? vence = null;
            if (s.FechaInicio != null)
                vence = bdSuscripcion.CalcularVencimiento(s.FechaInicio.Value);

            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", bdConexion.Valor(s.IdCliente)),
                new SqlParameter("@IdTipoMembresia", bdConexion.Valor(s.IdTipoMembresia)),
                new SqlParameter("@FechaInicio", bdConexion.Valor(s.FechaInicio)),
                new SqlParameter("@FechaVencimiento", bdConexion.Valor(vence)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSuscripcion", p));
        }

        // Accion 2: consulta general (filtros opcionales)
        public DataTable Consultar(string? busqueda, int? idTipoMembresia, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@Busqueda", bdConexion.Valor(busqueda)),
                new SqlParameter("@IdTipoMembresia", bdConexion.Valor(idTipoMembresia)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(idEstatus))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spSuscripcion", p);

            // Días que le quedan a cada suscripción
            dt.Columns.Add("DiasRestantes", typeof(int));
            foreach (DataRow fila in dt.Rows)
            {
                TimeSpan quedan = Convert.ToDateTime(fila["FechaVencimiento"]) - DateTime.Today;
                fila["DiasRestantes"] = quedan.Days;
            }
            return dt;
        }

        // Accion 3: cancelar
        public bdRespuesta Cancelar(int idSuscripcion, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@IdSuscripcion", idSuscripcion),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSuscripcion", p));
        }

        // Accion 4: renovar (encadena otro mes o reanuda la cancelada que todavía tiene días)
        public bdRespuesta Renovar(int idSuscripcion, int usuarioSesion)
        {
            // Accion 5: estatus y vencimiento de la suscripción que se renueva
            SqlParameter[] pa = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdSuscripcion", idSuscripcion)
            };
            DataTable actual = _bd.EjecutarStoredProcedure("spSuscripcion", pa);

            // Si sigue activa, el nuevo mes empieza al día siguiente de su vencimiento; si no, desde hoy
            DateTime inicio = DateTime.Today;
            if (actual.Rows.Count > 0 && Convert.ToInt32(actual.Rows[0]["IdEstatus"]) == 1)
                inicio = Convert.ToDateTime(actual.Rows[0]["FechaVencimiento"]).AddDays(1);
            DateTime vence = bdSuscripcion.CalcularVencimiento(inicio);

            // El SP valida todo; si es una cancelada que todavía tiene días, solo la reanuda (no usa estas fechas)
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdSuscripcion", idSuscripcion),
                new SqlParameter("@FechaInicio", inicio),
                new SqlParameter("@FechaVencimiento", vence),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSuscripcion", p));
        }

        // Convierte el Mensaje y TipoMensaje del SP en bdRespuesta.
        // Si el SP manda las fechas Desde / Hasta, se agregan al mensaje en formato dd/MM/yyyy.
        private bdRespuesta Respuesta(DataTable dt)
        {
            DataRow fila = dt.Rows[0];
            bdRespuesta r = new bdRespuesta
            {
                Mensaje = fila["Mensaje"].ToString() ?? "",
                TipoMensaje = fila["TipoMensaje"].ToString() ?? ""
            };

            if (dt.Columns.Contains("Desde") && fila["Desde"] != DBNull.Value)
                r.Mensaje = r.Mensaje + " Inicia el " + Convert.ToDateTime(fila["Desde"]).ToString("dd/MM/yyyy") + ".";
            if (dt.Columns.Contains("Hasta") && fila["Hasta"] != DBNull.Value)
                r.Mensaje = r.Mensaje + " Vence el " + Convert.ToDateTime(fila["Hasta"]).ToString("dd/MM/yyyy") + ".";

            return r;
        }
    }
}
