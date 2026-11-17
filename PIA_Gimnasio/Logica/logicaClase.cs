using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaClase
    {
        private readonly bdConexion _bd;

        public logicaClase(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1 (registrar) o 4 (modificar)
        public bdRespuesta Guardar(bdClase c, int usuarioSesion)
        {
            int accion = c.IdClase == 0 ? 1 : 4;

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdClase", c.IdClase),
                new SqlParameter("@Nombre", bdConexion.Valor(c.Nombre)),
                new SqlParameter("@Fecha", bdConexion.Valor(c.Fecha)),
                new SqlParameter("@HoraInicio", bdConexion.Valor(c.HoraInicio)),
                new SqlParameter("@HoraFin", bdConexion.Valor(c.HoraFin)),
                new SqlParameter("@Salon", bdConexion.Valor(c.Salon)),
                new SqlParameter("@CupoMaximo", bdConexion.Valor(c.CupoMaximo)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(c.IdEmpleado)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(c.IdSucursal)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spClase", p));
        }

        // Accion 2: leer un registro
        public bdClase? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdClase", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spClase", p);

            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new bdClase
            {
                IdClase = Convert.ToInt32(r["IdClase"]),
                Nombre = r["Nombre"].ToString(),
                Fecha = Convert.ToDateTime(r["Fecha"]),
                HoraInicio = (TimeSpan)r["HoraInicio"],
                HoraFin = (TimeSpan)r["HoraFin"],
                Salon = r["Salon"].ToString(),
                CupoMaximo = Convert.ToInt32(r["CupoMaximo"]),
                IdEmpleado = Convert.ToInt32(r["IdEmpleado"]),
                IdSucursal = Convert.ToInt32(r["IdSucursal"])
            };
        }

        // Accion 3: agenda de un rango de fechas
        public DataTable Consultar(DateTime desde, DateTime hasta, string? nombre, int? idSucursal, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Fecha", desde.Date),
                new SqlParameter("@FechaHasta", hasta.Date),
                new SqlParameter("@Nombre", bdConexion.Valor(nombre)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spClase", p);
        }

        // Accion 5: cancelar la clase y sus reservas
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdClase", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spClase", p));
        }

        private bdRespuesta Respuesta(DataTable dt)
        {
            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }
    }
}
