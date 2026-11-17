using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Expediente médico (spExpedienteMedico)
    public class logicaExpediente
    {
        private readonly bdConexion _bd;

        public logicaExpediente(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: registrar o actualizar el expediente del socio
        public bdRespuesta Guardar(bdExpediente e, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", e.IdCliente),
                new SqlParameter("@TipoSangre", bdConexion.Valor(e.TipoSangre)),
                new SqlParameter("@PadecimientosCronicos", bdConexion.Valor(e.PadecimientosCronicos)),
                new SqlParameter("@LesionesPrevias", bdConexion.Valor(e.LesionesPrevias)),
                new SqlParameter("@Alergias", bdConexion.Valor(e.Alergias)),
                new SqlParameter("@CondicionRiesgo", bdConexion.Valor(e.CondicionRiesgo)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spExpedienteMedico", p);

            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }

        // Accion 2: datos del socio + expediente (las columnas del expediente vienen en NULL si no tiene).
        // idSucursal (recepcionista) e idEmpleado (entrenador) limitan a qué socios puede ver; null = todos.
        public DataRow? Obtener(int idCliente, int? idSucursal, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdCliente", idCliente),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spExpedienteMedico", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 3: socios con o sin expediente (búsqueda por folio o nombre; mismos límites que Obtener)
        public DataTable Consultar(string? busqueda, int? idSucursal, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", bdConexion.Valor(busqueda)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spExpedienteMedico", p);
        }
    }
}
