using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaControlAcceso
    {
        private readonly bdConexion _bd;

        public logicaControlAcceso(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: aforo actual y máximo de la sucursal
        public DataRow? ObtenerAforo(int idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdSucursal", idSucursal)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spControlAcceso", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 2: registrar entrada.
        public DataRow RegistrarEntrada(int idSucursal, int folio, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdSucursal", idSucursal),
                new SqlParameter("@Folio", folio),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return _bd.EjecutarStoredProcedure("spControlAcceso", p).Rows[0];
        }

        // Accion 3: registrar salida
        public DataRow RegistrarSalida(int idSucursal, int folio, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@IdSucursal", idSucursal),
                new SqlParameter("@Folio", folio),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return _bd.EjecutarStoredProcedure("spControlAcceso", p).Rows[0];
        }

        // Accion 4: movimientos de hoy en la sucursal
        public DataTable ConsultarMovimientos(int idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdSucursal", idSucursal)
            };
            return _bd.EjecutarStoredProcedure("spControlAcceso", p);
        }
    }
}
