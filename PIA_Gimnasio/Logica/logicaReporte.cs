using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Clases del día y registro de asistencia (spPaseLista)
    public class logicaPaseLista
    {
        // Estatus de asistencia que maneja el pase de lista
        public const int Confirmado = 5;
        public const int Asistio = 6;
        public const int Falta = 7;

        private readonly bdConexion _bd;

        public logicaPaseLista(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: clases del día (idEmpleado = null: todas las clases, para el Administrador)
        public DataTable ConsultarClases(DateTime fecha, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@Fecha", fecha.Date),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spPaseLista", p);
        }

        // Accion 2: socios con reserva en la clase
        public DataTable ConsultarParticipantes(int idClase, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdClase", idClase),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spPaseLista", p);
        }

        // Accion 3: guardar asistencia. asistencia = IdReserva -> estatus (5, 6 o 7).
        // El detalle viaja al SP en JSON: [{"IdReserva":1,"IdEstatus":6}, ...]
        public bdRespuesta Guardar(int idClase, int? idEmpleado, Dictionary<int, int> asistencia, int usuarioSesion)
        {
            List<object> lista = new List<object>();
            foreach (KeyValuePair<int, int> a in asistencia)
            {
                lista.Add(new { IdReserva = a.Key, IdEstatus = a.Value });
            }
            string detalle = JsonSerializer.Serialize(lista);

            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@IdClase", idClase),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado)),
                new SqlParameter("@Detalle", detalle),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spPaseLista", p);

            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }

        // Manda NULL a SQL cuando el número no tiene valor
        private object Valor(int? numero)
        {
            return numero.HasValue ? numero.Value : DBNull.Value;
        }
    }

    // Los 5 reportes (spReporte)
    public class logicaReporte
    {
        private readonly bdConexion _bd;

        public logicaReporte(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: empleados y distribución por sucursal
        public DataTable Empleados(int? idSucursal, int? idPuesto, int? idEspecialidad, DateTime? desde, DateTime? hasta)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@IdPuesto", Valor(idPuesto)),
                new SqlParameter("@IdEspecialidad", Valor(idEspecialidad)),
                new SqlParameter("@FechaDesde", Valor(desde)),
                new SqlParameter("@FechaHasta", Valor(hasta))
            };
            return _bd.EjecutarStoredProcedure("spReporte", p);
        }

        // Accion 2: socios y estado de membresías (idEstatus: 1 Activa, 3 Vencida, 4 Cancelada, 0 Sin membresía)
        public DataTable Socios(int? idSucursal, int? idTipoMembresia, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@IdTipoMembresia", Valor(idTipoMembresia)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spReporte", p);
        }

        // Accion 3: control de acceso y uso de las instalaciones
        public DataTable ControlAcceso(string? busqueda, int? idSucursal, DateTime? desde, DateTime? hasta)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", Valor(busqueda)),
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@FechaDesde", Valor(desde)),
                new SqlParameter("@FechaHasta", Valor(hasta))
            };
            return _bd.EjecutarStoredProcedure("spReporte", p);
        }

        // Accion 4: clases grupales, reservas y asistencia
        public DataTable Clases(string? clase, int? idSucursal, int? idEmpleado, DateTime? desde, DateTime? hasta)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@Busqueda", Valor(clase)),
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado)),
                new SqlParameter("@FechaDesde", Valor(desde)),
                new SqlParameter("@FechaHasta", Valor(hasta))
            };
            return _bd.EjecutarStoredProcedure("spReporte", p);
        }

        // Accion 5: rutinas de entrenamiento y ejercicios asignados
        public DataTable Rutinas(string? socio, int? idEmpleado, string? objetivo, string? grupoMuscular, string? nivelDificultad)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@Busqueda", Valor(socio)),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado)),
                new SqlParameter("@Objetivo", Valor(objetivo)),
                new SqlParameter("@GrupoMuscular", Valor(grupoMuscular)),
                new SqlParameter("@NivelDificultad", Valor(nivelDificultad))
            };
            return _bd.EjecutarStoredProcedure("spReporte", p);
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
