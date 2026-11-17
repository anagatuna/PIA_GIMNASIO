using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Logica
{
    public class logicaInicio
    {
        private readonly bdConexion _bd;

        public logicaInicio(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: indicadores del día
        public DataRow? ObtenerIndicadores(int? idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@FechaLimite", DateTime.Today.AddDays(7))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spInicio", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 2: membresías que vencen en los próximos 7 días
        public DataTable ConsultarPorVencer(int? idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@FechaLimite", DateTime.Today.AddDays(7))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spInicio", p);

            // Días que le faltan a cada membresía para vencer
            dt.Columns.Add("Dias", typeof(int));
            foreach (DataRow fila in dt.Rows)
            {
                TimeSpan faltan = Convert.ToDateTime(fila["FechaVencimiento"]) - DateTime.Today;
                fila["Dias"] = faltan.Days;
            }
            return dt;
        }
    }
}
