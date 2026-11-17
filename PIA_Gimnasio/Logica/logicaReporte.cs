using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaReporte
    {
        private readonly bdConexion _bd;
        private readonly logicaEmpleado _logicaEmpleado;

        public logicaReporte(bdConexion bd, logicaEmpleado logicaEmpleado)
        {
            _bd = bd;
            _logicaEmpleado = logicaEmpleado;
        }

        // Accion 1: empleados y distribución por sucursal
        public DataTable Empleados(int? idSucursal, int? idPuesto, int? idEspecialidad, DateTime? desde, DateTime? hasta)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdPuesto", bdConexion.Valor(idPuesto)),
                new SqlParameter("@IdEspecialidad", bdConexion.Valor(idEspecialidad)),
                new SqlParameter("@FechaDesde", bdConexion.Valor(desde)),
                new SqlParameter("@FechaHasta", bdConexion.Valor(hasta))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spReporte", p);

            // Columna "Especialidades" con los nombres separados por coma
            logicaEmpleado.AgregarEspecialidades(dt, _logicaEmpleado.ConsultarEspecialidades(null));
            return dt;
        }

        // Accion 2: socios y estado de membresías
        public DataTable Socios(int? idSucursal, int? idTipoMembresia, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdTipoMembresia", bdConexion.Valor(idTipoMembresia)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spReporte", p);
        }

        // Accion 3: control de acceso y uso de las instalaciones
        public DataTable ControlAcceso(string? busqueda, int? idSucursal, DateTime? desde, DateTime? hasta)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", bdConexion.Valor(busqueda)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@FechaDesde", bdConexion.Valor(desde)),
                new SqlParameter("@FechaHasta", bdConexion.Valor(hasta))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spReporte", p);
            logicaControlAcceso.AgregarMinutos(dt);
            return dt;
        }

        // Accion 4: clases grupales, reservas y asistencia
        public DataTable Clases(string? clase, int? idSucursal, int? idEmpleado, DateTime? desde, DateTime? hasta, int? idEstatusReserva)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@Busqueda", bdConexion.Valor(clase)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(idEstatusReserva)),
                new SqlParameter("@FechaDesde", bdConexion.Valor(desde)),
                new SqlParameter("@FechaHasta", bdConexion.Valor(hasta))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spReporte", p);

            // Porcentajes con un decimal. La ocupación es reservas entre cupo máximo.
            // La efectividad es asistieron entre (asistieron + faltas); queda vacía si todavía no se pasa lista.
            dt.Columns.Add("Ocupacion", typeof(decimal));
            dt.Columns.Add("Efectividad", typeof(decimal));
            foreach (DataRow fila in dt.Rows)
            {
                int cupo = Convert.ToInt32(fila["CupoMaximo"]);
                int reservas = Convert.ToInt32(fila["Reservas"]);
                int asistieron = Convert.ToInt32(fila["Asistieron"]);
                int faltas = Convert.ToInt32(fila["Faltas"]);

                if (cupo > 0)
                    fila["Ocupacion"] = Math.Round(reservas * 100m / cupo, 1);
                else
                    fila["Ocupacion"] = 0m;

                if (asistieron + faltas > 0)
                    fila["Efectividad"] = Math.Round(asistieron * 100m / (asistieron + faltas), 1);
                else
                    fila["Efectividad"] = DBNull.Value;
            }
            return dt;
        }

        // Accion 5: rutinas de entrenamiento asignadas 
        public DataTable Rutinas(string? socio, int? idEmpleado, string? objetivo, string? grupoMuscular, string? nivelDificultad, DateTime? desde, DateTime? hasta)
        {
            return ConsultarRutinas(5, socio, idEmpleado, objetivo, grupoMuscular, nivelDificultad, desde, hasta);
        }

        // Accion 6: ejercicios de esas rutinas 
        public DataTable RutinasDetalle(string? socio, int? idEmpleado, string? objetivo, string? grupoMuscular, string? nivelDificultad, DateTime? desde, DateTime? hasta)
        {
            return ConsultarRutinas(6, socio, idEmpleado, objetivo, grupoMuscular, nivelDificultad, desde, hasta);
        }

        // Las acciones 5 y 6 usan los mismos filtros
        private DataTable ConsultarRutinas(int accion, string? socio, int? idEmpleado, string? objetivo, string? grupoMuscular, string? nivelDificultad, DateTime? desde, DateTime? hasta)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@Busqueda", bdConexion.Valor(socio)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado)),
                new SqlParameter("@Objetivo", bdConexion.Valor(objetivo)),
                new SqlParameter("@GrupoMuscular", bdConexion.Valor(grupoMuscular)),
                new SqlParameter("@NivelDificultad", bdConexion.Valor(nivelDificultad)),
                new SqlParameter("@FechaDesde", bdConexion.Valor(desde)),
                new SqlParameter("@FechaHasta", bdConexion.Valor(hasta))
            };
            return _bd.EjecutarStoredProcedure("spReporte", p);
        }
    }
}
