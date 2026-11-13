using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    public class logicaRutina
    {
        // Valores permitidos (también se validan en spRutina)
        public static List<string> DiasSemana = new List<string> { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        public static List<string> Objetivos = new List<string> { "Hipertrofia", "Pérdida de peso", "Reacondicionamiento", "Fuerza", "Resistencia" };

        private readonly bdConexion _bd;

        public logicaRutina(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: registrar cabecera + detalle en una sola transacción (el detalle viaja en JSON)
        public bdRespuesta Guardar(bdRutina rutina, int usuarioSesion)
        {
            string detalle = JsonSerializer.Serialize(rutina.Detalles);

            SqlParameter[] p = {
                new SqlParameter("@Accion", 1),
                new SqlParameter("@IdCliente", Valor(rutina.IdCliente)),
                new SqlParameter("@IdEmpleado", Valor(rutina.IdEmpleado)),
                new SqlParameter("@Nombre", Valor(rutina.Nombre?.Trim().ToUpper())),
                new SqlParameter("@Objetivo", Valor(rutina.Objetivo)),
                new SqlParameter("@FechaAsignacion", (object?)rutina.FechaAsignacion ?? DBNull.Value),
                new SqlParameter("@Detalle", detalle),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spRutina", p);

            bdRespuesta r = Respuesta(dt);
            r.Id = Convert.ToInt32(dt.Rows[0]["IdRutina"]);
            return r;
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
                new SqlParameter("@IdCliente", Valor(idCliente)),
                new SqlParameter("@IdEmpleado", Valor(idEmpleado)),
                new SqlParameter("@Objetivo", Valor(objetivo))
            };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 5: eliminar (baja lógica)
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdRutina", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spRutina", p));
        }

        // Accion 6: ejercicios de una rutina (de Lunes a Domingo)
        public DataTable ConsultarDetalle(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 6),
                new SqlParameter("@IdRutina", id)
            };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 7: socios activos (con bandera de expediente médico)
        public DataTable ConsultarSocios()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 7) };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 8: entrenadores activos
        public DataTable ConsultarEntrenadores()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 8) };
            return _bd.EjecutarStoredProcedure("spRutina", p);
        }

        // Accion 9: expediente médico del socio (null si no tiene)
        public DataRow? ObtenerExpediente(int idCliente)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 9),
                new SqlParameter("@IdCliente", idCliente)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spRutina", p);

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // ---------- Verificación médica ----------
        // Compara la contraindicación de cada ejercicio contra el expediente del socio.
        // Ejemplo: el ejercicio dice "Rodilla, Lumbar" y el socio tiene en lesiones previas "lesión de rodilla".

        // Campos del expediente que se revisan y cómo se nombran en la alerta
        private static string[] CamposMedicos = { "PadecimientosCronicos", "LesionesPrevias", "Alergias", "CondicionRiesgo" };
        private static string[] NombresCampos = { "padecimientos crónicos", "lesiones previas", "alergias", "condición de riesgo" };

        // Palabras demasiado generales para considerarlas coincidencia por sí solas
        private static List<string> PalabrasGenerales = new List<string>
        {
            "lesion", "lesiones", "dolor", "problema", "problemas", "cronico", "cronica", "previa", "previas",
            "condicion", "riesgo", "enfermedad", "sindrome", "leve", "grave", "severa", "severo", "ninguna", "ninguno", "n a"
        };

        // Regresa el texto de la alerta si el ejercicio está contraindicado para el socio; si no, null.
        public static string? RevisarContraindicacion(DataRow? expediente, string ejercicio, string? contraIndicacion)
        {
            if (expediente == null)
                return null;

            foreach (string termino in Terminos(contraIndicacion))
            {
                for (int i = 0; i < CamposMedicos.Length; i++)
                {
                    string valor = expediente[CamposMedicos[i]].ToString() ?? "";

                    foreach (string dato in Terminos(valor))
                    {
                        if (Coinciden(termino, dato))
                            return ejercicio + ": está contraindicado por \"" + termino + "\" y el socio registra en " + NombresCampos[i] + ": \"" + dato + "\".";
                    }
                }
            }
            return null;
        }

        // Separa un texto en términos: "Rodilla, hipertensión / asma" -> Rodilla | hipertensión | asma
        private static List<string> Terminos(string? texto)
        {
            List<string> terminos = new List<string>();
            if (string.IsNullOrWhiteSpace(texto))
                return terminos;

            string[] partes = texto.Replace(" y ", ",").Split(new char[] { ',', ';', '/' });
            foreach (string parte in partes)
            {
                string termino = parte.Trim();
                if (termino != "" && !PalabrasGenerales.Contains(Normalizar(termino).Trim()))
                    terminos.Add(termino);
            }
            return terminos;
        }

        // Coinciden si uno contiene al otro ("rodilla" / "lesión de rodilla")
        // o si comparten una palabra importante ("lesión de rodilla" / "rodilla operada")
        private static bool Coinciden(string a, string b)
        {
            string x = Normalizar(a);
            string y = Normalizar(b);

            if (x.Contains(y) || y.Contains(x))
                return true;

            List<string> palabrasY = new List<string>(y.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            foreach (string palabra in x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (palabra.Length >= 4 && !PalabrasGenerales.Contains(palabra) && palabrasY.Contains(palabra))
                    return true;
            }
            return false;
        }

        // Minúsculas y sin acentos, para comparar "Lesión" con "lesion"
        private static string Normalizar(string texto)
        {
            string resultado = texto.Trim().ToLower();
            resultado = resultado.Replace("á", "a").Replace("é", "e").Replace("í", "i")
                                 .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u");

            // Cualquier signo que no sea letra o número se cambia por espacio
            string limpio = "";
            foreach (char c in resultado)
            {
                if (char.IsLetterOrDigit(c))
                    limpio += c;
                else
                    limpio += ' ';
            }
            return limpio;
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

        // Manda NULL a SQL cuando el texto viene vacío
        private object Valor(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto;
        }

        // Manda NULL a SQL cuando el número no tiene valor
        private object Valor(int? numero)
        {
            return numero.HasValue ? numero.Value : DBNull.Value;
        }
    }
}
