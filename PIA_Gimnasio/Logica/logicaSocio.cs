using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Socios (spSocio). El registro incluye el expediente médico obligatorio.
    public class logicaSocio
    {
        public static List<string> TiposSangre = new List<string> { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };

        private readonly bdConexion _bd;

        public logicaSocio(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1 (registrar socio + expediente; el SP genera el folio) o 4 (modificar datos del socio)
        public bdRespuesta Guardar(bdSocio s, bdExpediente expediente, int usuarioSesion)
        {
            int accion = s.IdCliente == 0 ? 1 : 4;

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdCliente", s.IdCliente),
                new SqlParameter("@IdSucursal", Valor(s.IdSucursal)),
                new SqlParameter("@IdEstatus", Valor(s.IdEstatus)),
                new SqlParameter("@Nombre", Valor(s.Nombre?.Trim().ToUpper())),
                new SqlParameter("@PrimerApellido", Valor(s.PrimerApellido?.Trim().ToUpper())),
                new SqlParameter("@SegundoApellido", Valor(s.SegundoApellido?.Trim().ToUpper())),
                new SqlParameter("@FechaNacimiento", Valor(s.FechaNacimiento)),
                new SqlParameter("@Correo", Valor(s.Correo?.Trim().ToLower())),
                new SqlParameter("@Telefono", Valor(s.Telefono)),
                new SqlParameter("@TipoSangre", Valor(expediente.TipoSangre)),
                new SqlParameter("@PadecimientosCronicos", Valor(expediente.PadecimientosCronicos)),
                new SqlParameter("@LesionesPrevias", Valor(expediente.LesionesPrevias)),
                new SqlParameter("@Alergias", Valor(expediente.Alergias)),
                new SqlParameter("@CondicionRiesgo", Valor(expediente.CondicionRiesgo)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSocio", p));
        }

        // Accion 2: leer un registro
        public bdSocio? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdCliente", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spSocio", p);

            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new bdSocio
            {
                IdCliente = Convert.ToInt32(r["IdCliente"]),
                Folio = Convert.ToInt32(r["Folio"]),
                IdSucursal = Convert.ToInt32(r["IdSucursal"]),
                IdEstatus = Convert.ToInt32(r["IdEstatus"]),
                Nombre = r["Nombre"].ToString(),
                PrimerApellido = r["PrimerApellido"].ToString(),
                SegundoApellido = r["SegundoApellido"].ToString(),
                FechaNacimiento = Convert.ToDateTime(r["FechaNacimiento"]),
                Correo = r["Correo"].ToString(),
                Telefono = r["Telefono"].ToString(),
                FechaInscripcion = Convert.ToDateTime(r["FechaInscripcion"])
            };
        }

        // Accion 3: consulta general (búsqueda por folio o nombre)
        public DataTable Consultar(string? busqueda, int? idSucursal, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Nombre", Valor(busqueda)),
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spSocio", p);
        }

        // Accion 5: eliminar (baja lógica)
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdCliente", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSocio", p));
        }

        // Accion 6: socios activos (combos de suscripciones y reservas)
        public DataTable ConsultarActivos()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spSocio", p);
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
            return string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
        }

        // Manda NULL a SQL cuando el dato (número o fecha) no tiene valor
        private object Valor(object? dato)
        {
            return dato ?? DBNull.Value;
        }
    }

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
                new SqlParameter("@TipoSangre", Valor(e.TipoSangre)),
                new SqlParameter("@PadecimientosCronicos", Valor(e.PadecimientosCronicos)),
                new SqlParameter("@LesionesPrevias", Valor(e.LesionesPrevias)),
                new SqlParameter("@Alergias", Valor(e.Alergias)),
                new SqlParameter("@CondicionRiesgo", Valor(e.CondicionRiesgo)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spExpedienteMedico", p);

            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }

        // Accion 2: datos del socio + expediente (las columnas del expediente vienen en NULL si no tiene)
        public DataRow? Obtener(int idCliente)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdCliente", idCliente)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spExpedienteMedico", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 3: socios con o sin expediente (búsqueda por folio o nombre)
        public DataTable Consultar(string? busqueda)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", Valor(busqueda))
            };
            return _bd.EjecutarStoredProcedure("spExpedienteMedico", p);
        }

        // Manda NULL a SQL cuando el texto viene vacío
        private object Valor(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
        }
    }
}
