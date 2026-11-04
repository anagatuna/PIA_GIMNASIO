using System.Data;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Suscripciones (spSuscripcion): vigencia de 1 mes y máximo 1 activa por socio
    public class logicaSuscripcion : logicaBase
    {
        protected override string Procedimiento => "spSuscripcion";
        public logicaSuscripcion(bdConexion bd) : base(bd) { }

        public bdRespuesta Registrar(bdSuscripcion s, int usuarioSesion) =>
            Respuesta(1, ("@IdCliente", Valor(s.IdCliente)), ("@IdTipoMembresia", Valor(s.IdTipoMembresia)),
                ("@FechaInicio", Valor(s.FechaInicio)), ("@UsuarioSesion", usuarioSesion));

        public DataTable Consultar(string? busqueda, int? idTipoMembresia, int? idEstatus) =>
            Ejecutar(3, ("@Busqueda", Valor(busqueda)), ("@IdTipoMembresia", Valor(idTipoMembresia)), ("@IdEstatus", Valor(idEstatus)));

        public bdRespuesta Cancelar(int idSuscripcion, int usuarioSesion) =>
            Respuesta(4, ("@IdSuscripcion", idSuscripcion), ("@UsuarioSesion", usuarioSesion));

        public DataTable ConsultarEstatus() => Ejecutar(5);
    }

    // Entradas y salidas de socios (spControlAcceso)
    public class logicaControlAcceso : logicaBase
    {
        protected override string Procedimiento => "spControlAcceso";
        public logicaControlAcceso(bdConexion bd) : base(bd) { }

        // Aforo actual vs máximo de la sucursal
        public DataRow? ObtenerAforo(int idSucursal) => Renglon(1, ("@IdSucursal", idSucursal));

        // Resultado = Permitido (verde) / Denegado (rojo)
        public DataRow RegistrarEntrada(int idSucursal, int folio, int usuarioSesion) =>
            Renglon(2, ("@IdSucursal", idSucursal), ("@Folio", folio), ("@UsuarioSesion", usuarioSesion))!;

        // Resultado = Salida, con los minutos de permanencia calculados por el SP
        public DataRow RegistrarSalida(int idSucursal, int folio, int usuarioSesion) =>
            Renglon(3, ("@IdSucursal", idSucursal), ("@Folio", folio), ("@UsuarioSesion", usuarioSesion))!;

        // Movimientos de hoy en la sucursal
        public DataTable ConsultarMovimientos(int idSucursal) => Ejecutar(4, ("@IdSucursal", idSucursal));
    }

    // Programación de clases grupales (spClase)
    public class logicaClase : logicaBase
    {
        protected override string Procedimiento => "spClase";
        public logicaClase(bdConexion bd) : base(bd) { }

        public bdRespuesta Guardar(bdClase c, int usuarioSesion) =>
            Respuesta(c.IdClase == 0 ? 1 : 4,
                ("@IdClase", c.IdClase), ("@Nombre", Mayusculas(c.Nombre)), ("@Fecha", Valor(c.Fecha)),
                ("@HoraInicio", Valor(c.HoraInicio)), ("@HoraFin", Valor(c.HoraFin)), ("@Salon", Mayusculas(c.Salon)),
                ("@CupoMaximo", Valor(c.CupoMaximo)), ("@IdEmpleado", Valor(c.IdEmpleado)), ("@IdSucursal", Valor(c.IdSucursal)),
                ("@UsuarioSesion", usuarioSesion));

        public bdClase? Obtener(int id)
        {
            DataRow? r = Renglon(2, ("@IdClase", id));
            return r == null ? null : new bdClase
            {
                IdClase = Convert.ToInt32(r["IdClase"]),
                Nombre = Texto(r, "Nombre"),
                Fecha = Fecha(r, "Fecha"),
                HoraInicio = Hora(r, "HoraInicio"),
                HoraFin = Hora(r, "HoraFin"),
                Salon = Texto(r, "Salon"),
                CupoMaximo = Entero(r, "CupoMaximo"),
                IdEmpleado = Entero(r, "IdEmpleado"),
                IdSucursal = Entero(r, "IdSucursal")
            };
        }

        // Agenda de un rango de fechas
        public DataTable Consultar(DateTime desde, DateTime hasta, string? nombre, int? idSucursal, int? idEmpleado) =>
            Ejecutar(3, ("@Fecha", desde.Date), ("@FechaHasta", hasta.Date), ("@Nombre", Valor(nombre)),
                ("@IdSucursal", Valor(idSucursal)), ("@IdEmpleado", Valor(idEmpleado)));

        // Cancela la clase y sus reservas
        public bdRespuesta Eliminar(int id, int usuarioSesion) =>
            Respuesta(5, ("@IdClase", id), ("@UsuarioSesion", usuarioSesion));
    }

    // Reservas de clases (spReserva): cupo del salón + límite semanal del plan
    public class logicaReserva : logicaBase
    {
        protected override string Procedimiento => "spReserva";
        public logicaReserva(bdConexion bd) : base(bd) { }

        // idEmpleado = recepcionista que captura
        public bdRespuesta Registrar(bdReserva r, int idEmpleado, int usuarioSesion) =>
            Respuesta(1, ("@IdCliente", Valor(r.IdCliente)), ("@IdClase", Valor(r.IdClase)),
                ("@IdEmpleado", idEmpleado), ("@UsuarioSesion", usuarioSesion));

        public bdRespuesta Cancelar(int idReserva, int usuarioSesion) =>
            Respuesta(2, ("@IdReserva", idReserva), ("@UsuarioSesion", usuarioSesion));

        public DataTable Consultar(string? busqueda, int? idClase, DateTime? fecha, int? idEstatus) =>
            Ejecutar(3, ("@Busqueda", Valor(busqueda)), ("@IdClase", Valor(idClase)), ("@Fecha", Valor(fecha)), ("@IdEstatus", Valor(idEstatus)));

        // Clases futuras con lugares disponibles
        public DataTable ConsultarClasesDisponibles() => Ejecutar(4);

        // Plan del socio y clases reservadas en la semana de la fecha indicada
        public DataRow? ObtenerResumenSocio(int idCliente, DateTime? fecha = null) =>
            Renglon(5, ("@IdCliente", idCliente), ("@Fecha", Valor(fecha)));

        public DataTable ConsultarEstatus() => Ejecutar(6);
    }
}
