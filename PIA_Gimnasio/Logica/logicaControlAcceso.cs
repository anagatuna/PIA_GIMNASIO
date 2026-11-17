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
            DataRow fila = _bd.EjecutarStoredProcedure("spControlAcceso", p).Rows[0];

            // Si se registró la salida, se calculan los minutos de permanencia (salida menos entrada)
            if (fila["Resultado"].ToString() == "Salida")
            {
                TimeSpan permanencia = Convert.ToDateTime(fila["FechaSalida"]) - Convert.ToDateTime(fila["FechaEntrada"]);
                int minutos = (int)permanencia.TotalMinutes;
                fila["Minutos"] = minutos;
                fila["Mensaje"] = "Salida registrada. Permanencia: " + minutos + " minutos.";
            }
            return fila;
        }

        // Accion 4: movimientos de hoy en la sucursal
        public DataTable ConsultarMovimientos(int idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdSucursal", idSucursal)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spControlAcceso", p);
            AgregarMinutos(dt);
            return dt;
        }

        // Agrega la columna "Minutos": tiempo de permanencia de cada entrada.
        // Si el socio no ha salido, se cuenta hasta este momento. La usa también el reporte de control de acceso.
        public static void AgregarMinutos(DataTable dt)
        {
            dt.Columns.Add("Minutos", typeof(int));
            foreach (DataRow fila in dt.Rows)
            {
                DateTime entrada = Convert.ToDateTime(fila["FechaEntrada"]);
                DateTime salida = DateTime.Now;
                if (fila["FechaSalida"] != DBNull.Value)
                    salida = Convert.ToDateTime(fila["FechaSalida"]);

                TimeSpan permanencia = salida - entrada;
                fila["Minutos"] = (int)permanencia.TotalMinutes;
            }
        }
    }
}
