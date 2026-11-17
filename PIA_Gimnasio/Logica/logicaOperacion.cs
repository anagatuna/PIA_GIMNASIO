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
                new SqlParameter("@IdCliente", bdConexion.Valor(s.IdCliente)),
                new SqlParameter("@IdTipoMembresia", bdConexion.Valor(s.IdTipoMembresia)),
                new SqlParameter("@FechaInicio", bdConexion.Valor(s.FechaInicio)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSuscripcion", p));
        }

        // Accion 3: consulta general (filtros opcionales)
        public DataTable Consultar(string? busqueda, int? idTipoMembresia, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", bdConexion.Valor(busqueda)),
                new SqlParameter("@IdTipoMembresia", bdConexion.Valor(idTipoMembresia)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(idEstatus))
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

        // Accion 6: renovar (encadena otro mes o reanuda la cancelada que todavía tiene días)
        public bdRespuesta Renovar(int idSuscripcion, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 6),
                new SqlParameter("@IdSuscripcion", idSuscripcion),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSuscripcion", p));
        }

        // Convierte el Mensaje y TipoMensaje del SP en bdRespuesta.
        // Si el SP manda las fechas Desde / Hasta, se agregan al mensaje en formato dd/MM/yyyy.
        private bdRespuesta Respuesta(DataTable dt)
        {
            DataRow fila = dt.Rows[0];
            bdRespuesta r = new bdRespuesta
            {
                Mensaje = fila["Mensaje"].ToString() ?? "",
                TipoMensaje = fila["TipoMensaje"].ToString() ?? ""
            };

            if (dt.Columns.Contains("Desde") && fila["Desde"] != DBNull.Value)
                r.Mensaje = r.Mensaje + " Inicia el " + Convert.ToDateTime(fila["Desde"]).ToString("dd/MM/yyyy") + ".";
            if (dt.Columns.Contains("Hasta") && fila["Hasta"] != DBNull.Value)
                r.Mensaje = r.Mensaje + " Vence el " + Convert.ToDateTime(fila["Hasta"]).ToString("dd/MM/yyyy") + ".";

            return r;
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
                new SqlParameter("@Nombre", bdConexion.Valor(c.Nombre)),
                new SqlParameter("@Fecha", bdConexion.Valor(c.Fecha)),
                new SqlParameter("@HoraInicio", bdConexion.Valor(c.HoraInicio)),
                new SqlParameter("@HoraFin", bdConexion.Valor(c.HoraFin)),
                new SqlParameter("@Salon", bdConexion.Valor(c.Salon)),
                new SqlParameter("@CupoMaximo", bdConexion.Valor(c.CupoMaximo)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(c.IdEmpleado)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(c.IdSucursal)),
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
                new SqlParameter("@Nombre", bdConexion.Valor(nombre)),
                new SqlParameter("@IdSucursal", bdConexion.Valor(idSucursal)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado))
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
    }

    // Reservas de clases (spReserva): cupo del salón + límite semanal del plan
    public class logicaReserva
    {
        private readonly bdConexion _bd;

        public logicaReserva(bdConexion bd)
        {
            _bd = bd;
        }

        // Registrar reserva (idEmpleado = recepcionista que captura).
        // C# valida el límite semanal del plan (lunes a domingo de la clase);
        // el SP (Accion 1) valida la clase, el socio, el plan vigente, reservas repetidas y el cupo.
        public bdRespuesta Registrar(bdReserva r, int idEmpleado, int usuarioSesion)
        {
            // Plan vigente el día de la clase y cuántas clases lleva en esa semana
            DataRow? plan = ObtenerResumenSocio(Convert.ToInt32(r.IdCliente), r.IdClase);
            bool tienePlan = false;
            string membresia = "";
            int maximo = 0;
            int usadas = 0;

            if (plan != null && plan["Membresia"] != DBNull.Value)
            {
                tienePlan = true;
                membresia = plan["Membresia"].ToString() ?? "";
                maximo = Convert.ToInt32(plan["MaxClasesSemana"]);
                usadas = Convert.ToInt32(plan["Usadas"]);

                if (maximo > 0 && usadas >= maximo)
                {
                    return new bdRespuesta
                    {
                        Mensaje = "Límite semanal alcanzado: la membresía " + membresia + " permite " + maximo
                                  + " clases por semana y ya tiene " + usadas + ".",
                        TipoMensaje = "warning"
                    };
                }
            }

            // Accion 1: el SP valida lo demás y guarda
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", bdConexion.Valor(r.IdCliente)),
                new SqlParameter("@IdClase", bdConexion.Valor(r.IdClase)),
                new SqlParameter("@IdEmpleado", idEmpleado),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            bdRespuesta respuesta = Respuesta(_bd.EjecutarStoredProcedure("spReserva", p));

            // Al confirmar, el mensaje dice cuántas clases lleva y cuántas le quedan esa semana
            if (respuesta.TipoMensaje == "success" && tienePlan)
            {
                respuesta.Mensaje = "Reserva confirmada (" + (usadas + 1) + " de " + maximo + " clases de la semana, plan "
                                    + membresia + "). Clases restantes esa semana: " + (maximo - usadas - 1) + ".";
            }
            return respuesta;
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

        // Accion 3: consulta general (filtros opcionales).
        // Se agrega la columna "UsadasSemana": clases del socio en la semana de cada reserva.
        public DataTable Consultar(string? busqueda, int? idClase, DateTime? fecha, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Busqueda", bdConexion.Valor(busqueda)),
                new SqlParameter("@IdClase", bdConexion.Valor(idClase)),
                new SqlParameter("@Fecha", bdConexion.Valor(fecha)),
                new SqlParameter("@IdEstatus", bdConexion.Valor(idEstatus))
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spReserva", p);
            DataTable cuentan = ConsultarReservasQueCuentan();

            dt.Columns.Add("UsadasSemana", typeof(int));
            foreach (DataRow fila in dt.Rows)
            {
                fila["UsadasSemana"] = ContarEnSemana(cuentan, Convert.ToInt32(fila["IdCliente"]), Convert.ToDateTime(fila["Fecha"]));
            }
            return dt;
        }

        // Accion 4: clases futuras (las llenas solo se pueden reservar con plan Oro)
        public DataTable ConsultarClasesDisponibles()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 4) };
            DataTable dt = _bd.EjecutarStoredProcedure("spReserva", p);

            // Texto del combo: "YOGA · 20/11 08:00 · SUCURSAL CENTRO / SALON ZEN (3 lugares)"
            dt.Columns.Add("Clase");
            foreach (DataRow fila in dt.Rows)
            {
                int libres = Convert.ToInt32(fila["CupoMaximo"]) - Convert.ToInt32(fila["Reservadas"]);
                string lugares = " (" + libres + " lugares)";
                if (libres <= 0)
                    lugares = " (llena: solo Oro)";

                TimeSpan hora = (TimeSpan)fila["HoraInicio"];
                fila["Clase"] = fila["Nombre"] + " · " + Convert.ToDateTime(fila["Fecha"]).ToString("dd/MM") + " " + hora.ToString(@"hh\:mm")
                                + " · " + fila["Sucursal"] + " / " + fila["Salon"] + lugares;
            }
            return dt;
        }

        // Accion 5: plan del socio vigente en la fecha de la clase (sin clase, hoy).
        // Se agregan "Lunes" (inicio de esa semana) y "Usadas" (clases que lleva en esa semana).
        public DataRow? ObtenerResumenSocio(int idCliente, int? idClase)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdCliente", idCliente),
                new SqlParameter("@IdClase", bdConexion.Valor(idClase)),
                new SqlParameter("@Fecha", DBNull.Value)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spReserva", p);
            if (dt.Rows.Count == 0)
                return null;

            DataRow fila = dt.Rows[0];
            DateTime fechaClase = Convert.ToDateTime(fila["FechaClase"]);

            dt.Columns.Add("Lunes", typeof(DateTime));
            dt.Columns.Add("Usadas", typeof(int));
            fila["Lunes"] = Lunes(fechaClase);
            fila["Usadas"] = ContarEnSemana(ConsultarReservasQueCuentan(), idCliente, fechaClase);
            return fila;
        }

        // Accion 7: reservas que cuentan para el límite semanal (confirmada, asistió o falta) con la fecha de su clase
        private DataTable ConsultarReservasQueCuentan()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 7) };
            return _bd.EjecutarStoredProcedure("spReserva", p);
        }

        // Cuántas reservas tiene el socio en la semana (lunes a domingo) de esa fecha
        private int ContarEnSemana(DataTable cuentan, int idCliente, DateTime fecha)
        {
            DateTime lunes = Lunes(fecha);
            DateTime domingo = lunes.AddDays(6);

            int usadas = 0;
            foreach (DataRow r in cuentan.Rows)
            {
                DateTime fechaClase = Convert.ToDateTime(r["Fecha"]);
                if (Convert.ToInt32(r["IdCliente"]) == idCliente && fechaClase >= lunes && fechaClase <= domingo)
                    usadas++;
            }
            return usadas;
        }

        // Lunes de la semana de una fecha: se recorre hacia atrás hasta llegar al lunes
        private DateTime Lunes(DateTime fecha)
        {
            DateTime lunes = fecha.Date;
            while (lunes.DayOfWeek != DayOfWeek.Monday)
                lunes = lunes.AddDays(-1);
            return lunes;
        }

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
