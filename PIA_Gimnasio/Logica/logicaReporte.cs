using System.Data;
using System.Text.Json;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Agenda del entrenador y registro de asistencia (spPaseLista)
    public class logicaPaseLista : logicaBase
    {
        protected override string Procedimiento => "spPaseLista";
        public logicaPaseLista(bdConexion bd) : base(bd) { }

        // Estatus de asistencia que maneja el pase de lista
        public const int Confirmado = 5, Asistio = 6, Falta = 7;

        // idEmpleado = null: todas las clases (Administrador)
        public DataTable ConsultarClases(DateTime fecha, int? idEmpleado) =>
            Ejecutar(1, ("@Fecha", fecha.Date), ("@IdEmpleado", Valor(idEmpleado)));

        public DataTable ConsultarParticipantes(int idClase, int? idEmpleado) =>
            Ejecutar(2, ("@IdClase", idClase), ("@IdEmpleado", Valor(idEmpleado)));

        // asistencia: IdReserva -> estatus (5, 6 o 7)
        public bdRespuesta Guardar(int idClase, int? idEmpleado, Dictionary<int, int> asistencia, int usuarioSesion)
        {
            string detalle = JsonSerializer.Serialize(asistencia.Select(a => new { IdReserva = a.Key, IdEstatus = a.Value }));
            return Respuesta(3, ("@IdClase", idClase), ("@IdEmpleado", Valor(idEmpleado)),
                ("@Detalle", detalle), ("@UsuarioSesion", usuarioSesion));
        }
    }

    // Los 5 reportes operativos (spReporte)
    public class logicaReporte : logicaBase
    {
        protected override string Procedimiento => "spReporte";
        public logicaReporte(bdConexion bd) : base(bd) { }

        public DataTable Empleados(int? idSucursal, int? idPuesto, int? idEspecialidad, DateTime? desde, DateTime? hasta) =>
            Ejecutar(1, ("@IdSucursal", Valor(idSucursal)), ("@IdPuesto", Valor(idPuesto)), ("@IdEspecialidad", Valor(idEspecialidad)),
                ("@FechaDesde", Valor(desde)), ("@FechaHasta", Valor(hasta)));

        // idEstatus: 1 Activa, 3 Vencida, 4 Cancelada, 0 Sin membresía
        public DataTable Socios(int? idSucursal, int? idTipoMembresia, int? idEstatus) =>
            Ejecutar(2, ("@IdSucursal", Valor(idSucursal)), ("@IdTipoMembresia", Valor(idTipoMembresia)), ("@IdEstatus", Valor(idEstatus)));

        public DataTable ControlAcceso(string? busqueda, int? idSucursal, DateTime? desde, DateTime? hasta) =>
            Ejecutar(3, ("@Busqueda", Valor(busqueda)), ("@IdSucursal", Valor(idSucursal)),
                ("@FechaDesde", Valor(desde)), ("@FechaHasta", Valor(hasta)));

        public DataTable Clases(string? clase, int? idSucursal, int? idEmpleado, DateTime? desde, DateTime? hasta) =>
            Ejecutar(4, ("@Busqueda", Valor(clase)), ("@IdSucursal", Valor(idSucursal)), ("@IdEmpleado", Valor(idEmpleado)),
                ("@FechaDesde", Valor(desde)), ("@FechaHasta", Valor(hasta)));

        public DataTable Rutinas(string? socio, int? idEmpleado, string? objetivo, string? grupoMuscular, string? nivelDificultad) =>
            Ejecutar(5, ("@Busqueda", Valor(socio)), ("@IdEmpleado", Valor(idEmpleado)), ("@Objetivo", Valor(objetivo)),
                ("@GrupoMuscular", Valor(grupoMuscular)), ("@NivelDificultad", Valor(nivelDificultad)));
    }
}
