using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Clases del día y registro de asistencia (spPaseLista)
    public class logicaPaseLista
    {
        // Estatus de asistencia que maneja el pase de lista
        public const int Pendiente = 5;     // Reserva confirmada, sin asistencia registrada todavía
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
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spPaseLista", p);
        }

        // Accion 2: socios con reserva en la clase
        public DataTable ConsultarParticipantes(int idClase, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdClase", idClase),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spPaseLista", p);
        }

        // Guardar asistencia. asistencia = IdReserva -> estatus (5, 6 o 7).
        // Primero se validan todos los socios; si alguno tiene error no se guarda nada.
        public bdRespuesta Guardar(int idClase, int? idEmpleado, Dictionary<int, int> asistencia, int usuarioSesion)
        {
            // 1. Validar a todos (no guarda)
            string errores = "";
            foreach (KeyValuePair<int, int> a in asistencia)
            {
                bdRespuesta r = GuardarSocio(idClase, idEmpleado, a.Key, a.Value, true, usuarioSesion);
                if (r.TipoMensaje != "success" && !errores.Contains(r.Mensaje))
                    errores = errores + r.Mensaje + " ";
            }

            if (errores != "")
                return new bdRespuesta { Mensaje = errores.Trim(), TipoMensaje = "warning" };

            // 2. Guardar a todos
            int guardados = 0;
            foreach (KeyValuePair<int, int> a in asistencia)
            {
                bdRespuesta r = GuardarSocio(idClase, idEmpleado, a.Key, a.Value, false, usuarioSesion);
                if (r.TipoMensaje == "success")
                    guardados++;
            }

            return new bdRespuesta
            {
                Mensaje = "Pase de lista guardado (" + guardados + " participantes).",
                TipoMensaje = "success"
            };
        }

        // Accion 3: valida (soloValidar = true) o guarda la asistencia de un socio
        private bdRespuesta GuardarSocio(int idClase, int? idEmpleado, int idReserva, int idEstatus, bool soloValidar, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@IdClase", idClase),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado)),
                new SqlParameter("@IdReserva", idReserva),
                new SqlParameter("@IdEstatus", idEstatus),
                new SqlParameter("@SoloValidar", soloValidar),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spPaseLista", p);

            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }
    }

    // Los 5 reportes (spReporte)
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

        // Accion 2: socios y estado de membresías (idEstatus: 1 Activa, 3 Vencida, 4 Cancelada, 0 Sin membresía)
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
            return _bd.EjecutarStoredProcedure("spReporte", p);
        }

        // Accion 4: clases grupales, reservas y asistencia (idEstatusReserva: solo clases con alguna reserva en ese estatus)
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

            // Porcentajes con un decimal:
            //   Ocupación   = reservas que ocupan lugar / cupo máximo
            //   Efectividad = asistieron / (asistieron + faltas); vacía si todavía no se pasa lista
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

        // Accion 5: rutinas de entrenamiento asignadas (un renglón por rutina)
        public DataTable Rutinas(string? socio, int? idEmpleado, string? objetivo, string? grupoMuscular, string? nivelDificultad, DateTime? desde, DateTime? hasta)
        {
            return ConsultarRutinas(5, socio, idEmpleado, objetivo, grupoMuscular, nivelDificultad, desde, hasta);
        }

        // Accion 6: ejercicios de esas rutinas (un renglón por ejercicio: grupo, nivel, día, series, repeticiones y descanso)
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
