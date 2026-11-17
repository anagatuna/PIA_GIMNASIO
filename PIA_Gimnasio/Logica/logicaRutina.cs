using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaRutina
    {
        private readonly bdConexion _bd;

        public logicaRutina(bdConexion bd)
        {
            _bd = bd;
        }

        // Registrar la rutina con sus ejercicios. 
        public bdRespuesta Guardar(bdRutina rutina, int usuarioSesion)
        {
            if (rutina.Detalles.Count == 0)
                return new bdRespuesta { Mensaje = "Agrega al menos un ejercicio a la rutina.", TipoMensaje = "warning" };

            bdRespuesta r = GuardarCabecera(rutina, usuarioSesion);
            if (r.TipoMensaje != "success")
                return r;

            foreach (bdDetalleRutina d in rutina.Detalles)
            {
                GuardarEjercicio(r.Id, d);
            }
            return r;
        }

        // Accion 1: cabecera de la rutina
        private bdRespuesta GuardarCabecera(bdRutina rutina, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", bdConexion.Valor(rutina.IdCliente)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(rutina.IdEmpleado)),
                new SqlParameter("@Nombre", bdConexion.Valor(rutina.Nombre)),
                new SqlParameter("@Objetivo", bdConexion.Valor(rutina.Objetivo)),
                new SqlParameter("@FechaAsignacion", (object?)rutina.FechaAsignacion ?? DBNull.Value),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spRutina", p);

            bdRespuesta r = Respuesta(dt);
            r.Id = Convert.ToInt32(dt.Rows[0]["IdRutina"]);
            return r;
        }

        // Accion 9: agrega un ejercicio al detalle de la rutina
        private bdRespuesta GuardarEjercicio(int idRutina, bdDetalleRutina d)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 9),
                new SqlParameter("@IdRutina", idRutina),
                new SqlParameter("@IdEjercicio", d.IdEjercicio),
                new SqlParameter("@DiaSemana", bdConexion.Valor(d.DiaSemana)),
                new SqlParameter("@Series", d.Series),
                new SqlParameter("@Repeticiones", d.Repeticiones),
                new SqlParameter("@TiempoDescanso", d.TiempoDescanso)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spRutina", p));
        }

        // Accion 2: cabecera de una rutina
        public DataRow? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdRutina", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spRutina", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Accion 3: consulta general (filtros opcionales)
        public DataTable Consultar(int? idCliente, int? idEmpleado, string? objetivo)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@IdCliente", bdConexion.Valor(idCliente)),
                new SqlParameter("@IdEmpleado", bdConexion.Valor(idEmpleado)),
                new SqlParameter("@Objetivo", bdConexion.Valor(objetivo))
            };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 4: eliminar (baja lógica)
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 4),
                new SqlParameter("@IdRutina", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spRutina", p));
        }

        // Accion 5: ejercicios de una rutina (de Lunes a Domingo)
        public DataTable ConsultarDetalle(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdRutina", id)
            };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 6: socios activos 
        public DataTable ConsultarSocios()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 7: entrenadores activos
        public DataTable ConsultarEntrenadores()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 7) };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 8: expediente médico del socio 
        public DataRow? ObtenerExpediente(int idCliente)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 8),
                new SqlParameter("@IdCliente", idCliente)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spRutina", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Verificación médica: compara la contraindicación de cada ejercicio contra el expediente del socio.

        // Campos del expediente que se revisan y cómo se nombran en la alerta
        private static string[] CamposMedicos = { "PadecimientosCronicos", "LesionesPrevias", "Alergias", "CondicionRiesgo" };
        private static string[] NombresCampos = { "padecimientos crónicos", "lesiones previas", "alergias", "condición de riesgo" };

        // Regresa el texto de la alerta si el ejercicio está contraindicado para el socio
        public static string? RevisarContraindicacion(DataRow? expediente, string ejercicio, string? contraIndicacion)
        {
            if (expediente == null || string.IsNullOrWhiteSpace(contraIndicacion))
                return null;

            // La contraindicación viene separada por comas
            foreach (string parte in contraIndicacion.Split(','))
            {
                string termino = parte.Trim().ToUpper();
                if (termino == "" || termino == "NINGUNA")
                    continue;

                // Si algún campo del expediente menciona el término, el ejercicio está contraindicado
                for (int i = 0; i < CamposMedicos.Length; i++)
                {
                    string valor = (expediente[CamposMedicos[i]].ToString() ?? "").ToUpper();
                    if (valor.Contains(termino))
                        return ejercicio + ": está contraindicado por \"" + termino + "\" y el socio registra en " + NombresCampos[i] + ": \"" + valor + "\".";
                }
            }
            return null;
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
