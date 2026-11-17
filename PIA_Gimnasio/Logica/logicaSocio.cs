using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Socios (spSocio). El registro incluye el expediente médico obligatorio.
    public class logicaSocio
    {
        private readonly bdConexion _bd;

        public logicaSocio(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1 (registrar socio + expediente; el SP genera el folio) o 4 (modificar datos del socio)
        public bdRespuesta Guardar(bdSocio s, bdExpediente expediente, int usuarioSesion)
        {
            int accion = s.IdCliente == 0 ? 1 : 4;

            // El correo se guarda en minúsculas
            string correo = (s.Correo ?? "").ToLower();

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdCliente", s.IdCliente),
                new SqlParameter("@IdSucursal", bdConexion.Valor(s.IdSucursal)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(s.IdEstatus)),
                new SqlParameter("@Nombre", bdConexion.Valor(s.Nombre)),
                new SqlParameter("@PrimerApellido", bdConexion.Valor(s.PrimerApellido)),
                new SqlParameter("@SegundoApellido", bdConexion.Valor(s.SegundoApellido)),
                new SqlParameter("@FechaNacimiento", bdConexion.Valor(s.FechaNacimiento)),
                new SqlParameter("@Correo", bdConexion.Valor(correo)),
                new SqlParameter("@Telefono", bdConexion.Valor(s.Telefono)),
                new SqlParameter("@TipoSangre", bdConexion.Valor(expediente.TipoSangre)),
                new SqlParameter("@PadecimientosCronicos", bdConexion.Valor(expediente.PadecimientosCronicos)),
                new SqlParameter("@LesionesPrevias", bdConexion.Valor(expediente.LesionesPrevias)),
                new SqlParameter("@Alergias", bdConexion.Valor(expediente.Alergias)),
                new SqlParameter("@CondicionRiesgo", bdConexion.Valor(expediente.CondicionRiesgo)),
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

        // Accion 3: consulta general (búsqueda por folio o nombre; membresía 0 = sin membresía)
        public DataTable Consultar(string? busqueda, int? idSucursal, int? idEstatus, int? idTipoMembresia)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Nombre", bdConexion.Valor(busqueda)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(idEstatus)),
                new SqlParameter("@IdTipoMembresia", bdConexion.Valor(idTipoMembresia))
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

        // Convierte el Mensaje y TipoMensaje del SP en bdRespuesta.
        // Al registrar, el SP también regresa el IdCliente del socio nuevo.
        private bdRespuesta Respuesta(DataTable dt)
        {
            bdRespuesta r = new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
            if (dt.Columns.Contains("IdCliente"))
                r.Id = Convert.ToInt32(dt.Rows[0]["IdCliente"]);
            return r;
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
