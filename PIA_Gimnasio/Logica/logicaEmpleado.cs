using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Empleados y sus especialidades (spEmpleado)
    public class logicaEmpleado
    {
        private readonly bdConexion _bd;

        public logicaEmpleado(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1 (registrar) o 4 (modificar).
        // Las especialidades viajan como "1,3,5"; el SP las ignora si el puesto no es Entrenador.
        public bdRespuesta Guardar(bdEmpleado e, int usuarioSesion)
        {
            int accion = e.IdEmpleado == 0 ? 1 : 4;

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdEmpleado", e.IdEmpleado),
                new SqlParameter("@IdSucursal", Valor(e.IdSucursal)),
                new SqlParameter("@IdPuesto", Valor(e.IdPuesto)),
                new SqlParameter("@IdEstatus", Valor(e.IdEstatus)),
                new SqlParameter("@Nombre", Valor(e.Nombre?.Trim().ToUpper())),
                new SqlParameter("@PrimerApellido", Valor(e.PrimerApellido?.Trim().ToUpper())),
                new SqlParameter("@SegundoApellido", Valor(e.SegundoApellido?.Trim().ToUpper())),
                new SqlParameter("@CURP", Valor(e.CURP?.Trim().ToUpper())),
                new SqlParameter("@RFC", Valor(e.RFC?.Trim().ToUpper())),
                new SqlParameter("@FechaNacimiento", Valor(e.FechaNacimiento)),
                new SqlParameter("@Correo", Valor(e.Correo?.Trim().ToLower())),
                new SqlParameter("@Telefono", Valor(e.Telefono)),
                new SqlParameter("@FechaContratacion", Valor(e.FechaContratacion)),
                new SqlParameter("@Especialidades", string.Join(",", e.Especialidades)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spEmpleado", p));
        }

        // Accion 2: leer un registro
        public bdEmpleado? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdEmpleado", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spEmpleado", p);

            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            bdEmpleado empleado = new bdEmpleado
            {
                IdEmpleado = Convert.ToInt32(r["IdEmpleado"]),
                IdSucursal = Convert.ToInt32(r["IdSucursal"]),
                IdPuesto = Convert.ToInt32(r["IdPuesto"]),
                IdEstatus = Convert.ToInt32(r["IdEstatus"]),
                Nombre = r["Nombre"].ToString(),
                PrimerApellido = r["PrimerApellido"].ToString(),
                SegundoApellido = r["SegundoApellido"].ToString(),
                CURP = r["CURP"].ToString(),
                RFC = r["RFC"].ToString(),
                FechaNacimiento = Convert.ToDateTime(r["FechaNacimiento"]),
                Correo = r["Correo"].ToString(),
                Telefono = r["Telefono"].ToString(),
                FechaContratacion = Convert.ToDateTime(r["FechaContratacion"])
            };

            // Las especialidades vienen como "1,3,5"
            string especialidades = r["Especialidades"].ToString() ?? "";
            foreach (string idEspecialidad in especialidades.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                empleado.Especialidades.Add(Convert.ToInt32(idEspecialidad));
            }

            return empleado;
        }

        // Accion 3: consulta general (filtros opcionales)
        public DataTable Consultar(string? nombre, int? idSucursal, int? idPuesto, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Nombre", Valor(nombre)),
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@IdPuesto", Valor(idPuesto)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spEmpleado", p);
        }

        // Accion 5: eliminar (baja lógica)
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdEmpleado", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spEmpleado", p));
        }

        // Accion 6: combo de puestos
        public DataTable ConsultarPuestos()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spEmpleado", p);
        }

        // Accion 7: lista de especialidades
        public DataTable ConsultarEspecialidades()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 7) };
            return _bd.EjecutarStoredProcedure("spEmpleado", p);
        }

        // Accion 8: entrenadores activos (combos de clases y reportes)
        public DataTable ConsultarEntrenadores()
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 8),
                new SqlParameter("@IdSucursal", DBNull.Value)
            };
            return _bd.EjecutarStoredProcedure("spEmpleado", p);
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
}
