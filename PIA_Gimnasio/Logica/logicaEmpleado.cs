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

        // Accion 1 (registrar) o 4 (modificar). Si sale bien, guarda cada especialidad marcada (Accion 7);
        // el SP solo las guarda si el puesto es Entrenador.
        public bdRespuesta Guardar(bdEmpleado e, int usuarioSesion)
        {
            int accion = e.IdEmpleado == 0 ? 1 : 4;

            // El correo se guarda en minúsculas
            string correo = (e.Correo ?? "").ToLower();

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdEmpleado", e.IdEmpleado),
                new SqlParameter("@IdSucursal", bdConexion.Valor(e.IdSucursal)),
                new SqlParameter("@IdPuesto", bdConexion.Valor(e.IdPuesto)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(e.IdEstatus)),
                new SqlParameter("@Nombre", bdConexion.Valor(e.Nombre)),
                new SqlParameter("@PrimerApellido", bdConexion.Valor(e.PrimerApellido)),
                new SqlParameter("@SegundoApellido", bdConexion.Valor(e.SegundoApellido)),
                new SqlParameter("@CURP", bdConexion.Valor(e.CURP)),
                new SqlParameter("@RFC", bdConexion.Valor(e.RFC)),
                new SqlParameter("@FechaNacimiento", bdConexion.Valor(e.FechaNacimiento)),
                new SqlParameter("@Correo", bdConexion.Valor(correo)),
                new SqlParameter("@Telefono", bdConexion.Valor(e.Telefono)),
                new SqlParameter("@FechaContratacion", bdConexion.Valor(e.FechaContratacion)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spEmpleado", p);
            bdRespuesta respuesta = Respuesta(dt);
            if (respuesta.TipoMensaje != "success")
                return respuesta;

            // Al registrar, el SP regresa el Id del empleado nuevo
            int idEmpleado = e.IdEmpleado;
            if (accion == 1)
                idEmpleado = Convert.ToInt32(dt.Rows[0]["Id"]);

            foreach (int idEspecialidad in e.Especialidades)
            {
                SqlParameter[] pe = {
                    new SqlParameter("@Accion", 7),
                    new SqlParameter("@IdEmpleado", idEmpleado),
                    new SqlParameter("@IdEspecialidad", idEspecialidad),
                    new SqlParameter("@UsuarioSesion", usuarioSesion)
                };
                _bd.EjecutarStoredProcedure("spEmpleado", pe);
            }
            return respuesta;
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
                new SqlParameter("@Nombre", bdConexion.Valor(nombre)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdPuesto", bdConexion.Valor(idPuesto)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(idEstatus))
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
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
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
    }
}
