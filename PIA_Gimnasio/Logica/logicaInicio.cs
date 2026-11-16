using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Logica
{
    // Resumen de la pantalla de inicio (spInicio). idSucursal NULL = todas las sucursales.
    public class logicaInicio
    {
        private readonly bdConexion _bd;

        public logicaInicio(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: indicadores del día (un solo renglón)
        public DataRow? ObtenerIndicadores(int? idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdSucursal", Valor(idSucursal))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spInicio", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 2: membresías que vencen en los próximos 7 días
        public DataTable ConsultarPorVencer(int? idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdSucursal", Valor(idSucursal))
            };
            return _bd.EjecutarStoredProcedure("spInicio", p);
        }

        // Manda NULL a SQL cuando el dato no tiene valor
        private object Valor(object? dato)
        {
            return dato ?? DBNull.Value;
        }
    }
}
