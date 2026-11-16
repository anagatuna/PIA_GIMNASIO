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

            // Sus especialidades (un renglón por cada una)
            foreach (DataRow esp in ConsultarEspecialidades(id).Rows)
            {
                empleado.Especialidades.Add(Convert.ToInt32(esp["IdEspecialidad"]));
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
            DataTable dt = _bd.EjecutarStoredProcedure("spEmpleado", p);

            // Columna "Especialidades" con los nombres separados por coma (ej. "Pesas, Yoga")
            AgregarEspecialidades(dt, ConsultarEspecialidades(null));
            return dt;
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

        // Accion 6: especialidades de los empleados, un renglón por especialidad (idEmpleado null = de todos)
        public DataTable ConsultarEspecialidades(int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 6),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spEmpleado", p);
        }

        // Accion 8: entrenadores activos (combos de clases y reportes).
        // Al nombre se le agregan sus especialidades: "CARLOS RAMIREZ (Pesas, CrossFit)"
        public DataTable ConsultarEntrenadores()
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 8),
                new SqlParameter("@IdSucursal", DBNull.Value)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spEmpleado", p);
            DataTable especialidades = ConsultarEspecialidades(null);

            foreach (DataRow fila in dt.Rows)
            {
                string lista = JuntarEspecialidades(especialidades, Convert.ToInt32(fila["IdEmpleado"]));
                if (lista != "")
                    fila["Entrenador"] = fila["Entrenador"] + " (" + lista + ")";
            }
            return dt;
        }

        // Agrega a la tabla la columna "Especialidades" (nombres separados por coma; vacía = NULL).
        // La tabla debe traer la columna IdEmpleado. La usa también el reporte de empleados.
        public static void AgregarEspecialidades(DataTable dt, DataTable especialidades)
        {
            dt.Columns.Add("Especialidades", typeof(string));
            foreach (DataRow fila in dt.Rows)
            {
                string lista = JuntarEspecialidades(especialidades, Convert.ToInt32(fila["IdEmpleado"]));
                if (lista == "")
                    fila["Especialidades"] = DBNull.Value;
                else
                    fila["Especialidades"] = lista;
            }
        }

        // Junta los nombres de las especialidades de un empleado: "Pesas, Yoga"
        private static string JuntarEspecialidades(DataTable especialidades, int idEmpleado)
        {
            string lista = "";
            foreach (DataRow esp in especialidades.Rows)
            {
                if (Convert.ToInt32(esp["IdEmpleado"]) == idEmpleado)
                {
                    if (lista != "")
                        lista = lista + ", ";
                    lista = lista + esp["Nombre"];
                }
            }
            return lista;
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
