using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Suscripciones (spSuscripcion): vigencia de 1 mes y máximo 1 activa por socio
    public class logicaSuscripcion
    {
        private readonly bdConexion _bd;

        public logicaSuscripcion(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: registrar (el SP calcula la fecha de vencimiento)
        public bdRespuesta Registrar(bdSuscripcion s, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", Valor(s.IdCliente)),
                new SqlParameter("@IdTipoMembresia", Valor(s.IdTipoMembresia)),
                new SqlParameter("@FechaInicio", Valor(s.FechaInicio)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSuscripcion", p));
        }

        // Accion 3: consulta general (filtros opcionales)
        public DataTable Consultar(string? busqueda, int? idTipoMembresia, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", Valor(busqueda)),
                new SqlParameter("@IdTipoMembresia", Valor(idTipoMembresia)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spSuscripcion", p);
        }

        // Accion 4: cancelar
        public bdRespuesta Cancelar(int idSuscripcion, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdSuscripcion", idSuscripcion),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSuscripcion", p));
        }

        // Accion 5: combo de estatus (Activa, Vencida, Cancelada)
        public DataTable ConsultarEstatus()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 5) };
            return _bd.EjecutarStoredProcedure("spSuscripcion", p);
        }

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

    // Entradas y salidas de socios (spControlAcceso)
    public class logicaControlAcceso
    {
        private readonly bdConexion _bd;

        public logicaControlAcceso(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: aforo actual vs máximo de la sucursal
        public DataRow? ObtenerAforo(int idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdSucursal", idSucursal)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spControlAcceso", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 2: registrar entrada. Resultado = Permitido (verde) o Denegado (rojo)
        public DataRow RegistrarEntrada(int idSucursal, int folio, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdSucursal", idSucursal),
                new SqlParameter("@Folio", folio),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return _bd.EjecutarStoredProcedure("spControlAcceso", p).Rows[0];
        }

        // Accion 3: registrar salida (el SP calcula los minutos de permanencia)
        public DataRow RegistrarSalida(int idSucursal, int folio, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@IdSucursal", idSucursal),
                new SqlParameter("@Folio", folio),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return _bd.EjecutarStoredProcedure("spControlAcceso", p).Rows[0];
        }

        // Accion 4: movimientos de hoy en la sucursal
        public DataTable ConsultarMovimientos(int idSucursal)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdSucursal", idSucursal)
            };
            return _bd.EjecutarStoredProcedure("spControlAcceso", p);
        }
    }

    // Programación de clases grupales (spClase)
    public class logicaClase
    {
        private readonly bdConexion _bd;

        public logicaClase(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1 (registrar) o 4 (modificar)
        public bdRespuesta Guardar(bdClase c, int usuarioSesion)
        {
            int accion = c.IdClase == 0 ? 1 : 4;

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdClase", c.IdClase),
                new SqlParameter("@Nombre", Valor(c.Nombre?.Trim().ToUpper())),
                new SqlParameter("@Fecha", Valor(c.Fecha)),
                new SqlParameter("@HoraInicio", Valor(c.HoraInicio)),
                new SqlParameter("@HoraFin", Valor(c.HoraFin)),
                new SqlParameter("@Salon", Valor(c.Salon?.Trim().ToUpper())),
                new SqlParameter("@CupoMaximo", Valor(c.CupoMaximo)),
                new SqlParameter("@IdEmpleado", Valor(c.IdEmpleado)),
                new SqlParameter("@IdSucursal", Valor(c.IdSucursal)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spClase", p));
        }

        // Accion 2: leer un registro
        public bdClase? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdClase", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spClase", p);

            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new bdClase
            {
                IdClase = Convert.ToInt32(r["IdClase"]),
                Nombre = r["Nombre"].ToString(),
                Fecha = Convert.ToDateTime(r["Fecha"]),
                HoraInicio = (TimeSpan)r["HoraInicio"],
                HoraFin = (TimeSpan)r["HoraFin"],
                Salon = r["Salon"].ToString(),
                CupoMaximo = Convert.ToInt32(r["CupoMaximo"]),
                IdEmpleado = Convert.ToInt32(r["IdEmpleado"]),
                IdSucursal = Convert.ToInt32(r["IdSucursal"])
            };
        }

        // Accion 3: agenda de un rango de fechas
        public DataTable Consultar(DateTime desde, DateTime hasta, string? nombre, int? idSucursal, int? idEmpleado)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Fecha", desde.Date),
                new SqlParameter("@FechaHasta", hasta.Date),
                new SqlParameter("@Nombre", Valor(nombre)),
                new SqlParameter("@IdSucursal", Valor(idSucursal)),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado))
            };
            return _bd.EjecutarStoredProcedure("spClase", p);
        }

        // Accion 5: cancelar la clase y sus reservas
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdClase", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spClase", p));
        }

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

        // Manda NULL a SQL cuando el dato (número, fecha u hora) no tiene valor
        private object Valor(object? dato)
        {
            return dato ?? DBNull.Value;
        }
    }

    // Reservas de clases (spReserva): cupo del salón + límite semanal del plan
    public class logicaReserva
    {
        private readonly bdConexion _bd;

        public logicaReserva(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: registrar (idEmpleado = recepcionista que captura)
        public bdRespuesta Registrar(bdReserva r, int idEmpleado, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", Valor(r.IdCliente)),
                new SqlParameter("@IdClase", Valor(r.IdClase)),
                new SqlParameter("@IdEmpleado", idEmpleado),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spReserva", p));
        }

        // Accion 2: cancelar
        public bdRespuesta Cancelar(int idReserva, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdReserva", idReserva),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spReserva", p));
        }

        // Accion 3: consulta general (filtros opcionales)
        public DataTable Consultar(string? busqueda, int? idClase, DateTime? fecha, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", Valor(busqueda)),
                new SqlParameter("@IdClase", Valor(idClase)),
                new SqlParameter("@Fecha", Valor(fecha)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spReserva", p);
        }

        // Accion 4: clases futuras con lugares disponibles
        public DataTable ConsultarClasesDisponibles()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 4) };
            return _bd.EjecutarStoredProcedure("spReserva", p);
        }

        // Accion 5: plan del socio y clases reservadas en la semana actual
        public DataRow? ObtenerResumenSocio(int idCliente)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdCliente", idCliente),
                new SqlParameter("@Fecha", DBNull.Value)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spReserva", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 6: combo de estatus de reserva
        public DataTable ConsultarEstatus()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spReserva", p);
        }

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
