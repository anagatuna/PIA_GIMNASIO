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
                new SqlParameter("@IdEmpleado", Valor(idEmpleado)),
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
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@IdPuesto", Valor(idPuesto)),
                new SqlParameter("@IdEspecialidad", Valor(idEspecialidad)),
                new SqlParameter("@FechaDesde", Valor(desde)),
                new SqlParameter("@FechaHasta", Valor(hasta))
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
            DataTable dt = _bd.EjecutarStoredProcedure("spReporte", p);

            // Accion 6: ejercicios de las rutinas, un renglón por ejercicio
            SqlParameter[] p2 = { new SqlParameter("@Accion", 6) };
            DataTable ejercicios = _bd.EjecutarStoredProcedure("spReporte", p2);

            // Columna "Ejercicios" con los nombres separados por coma
            dt.Columns.Add("Ejercicios", typeof(string));
            foreach (DataRow fila in dt.Rows)
            {
                string lista = "";
                foreach (DataRow ej in ejercicios.Rows)
                {
                    if (Convert.ToInt32(ej["IdRutina"]) == Convert.ToInt32(fila["IdRutina"]))
                    {
                        if (lista != "")
                            lista = lista + ", ";
                        lista = lista + ej["Nombre"];
                    }
                }
                fila["Ejercicios"] = lista;
            }
            return dt;
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
