using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaReserva
    {
        private readonly bdConexion _bd;

        public logicaReserva(bdConexion bd)
        {
            _bd = bd;
        }

        // Registrar reserva 
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

        // Accion 4: clases futuras
        public DataTable ConsultarClasesDisponibles()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 4) };
            DataTable dt = _bd.EjecutarStoredProcedure("spReserva", p);

            // Texto del combo
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

        // Accion 6: reservas que cuentan para el límite semanal
        private DataTable ConsultarReservasQueCuentan()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
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
