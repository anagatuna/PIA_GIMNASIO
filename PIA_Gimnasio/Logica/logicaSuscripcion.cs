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
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", bdConexion.Valor(s.IdCliente)),
                new SqlParameter("@IdTipoMembresia", bdConexion.Valor(s.IdTipoMembresia)),
                new SqlParameter("@FechaInicio", bdConexion.Valor(s.FechaInicio)),
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
            return _bd.EjecutarStoredProcedure("spSuscripcion", p);
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
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdSuscripcion", idSuscripcion),
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
