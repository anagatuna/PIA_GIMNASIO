using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
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

        // Accion 1: registrar cabecera + detalle en una sola transacción (el detalle viaja en JSON)
        public (bdRespuesta Respuesta, int IdRutina) Guardar(bdRutina rutina, int usuarioSesion)
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

            return (Respuesta(dt), Convert.ToInt32(dt.Rows[0]["IdRutina"]));
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

        #region Verificación médica

        // Campos del expediente que se comparan contra las contraindicaciones
        private static readonly (string Columna, string Etiqueta)[] CamposMedicos =
        {
            ("PadecimientosCronicos", "Padecimientos crónicos"),
            ("LesionesPrevias", "Lesiones previas"),
            ("Alergias", "Alergias"),
            ("CondicionRiesgo", "Condición de riesgo")
        };

        // Palabras demasiado generales para considerarlas coincidencia por sí solas
        private static readonly HashSet<string> PalabrasGenerales = new()
        {
            "lesion", "lesiones", "dolor", "problema", "problemas", "cronico", "cronica", "previa", "previas",
            "condicion", "riesgo", "enfermedad", "sindrome", "leve", "grave", "severa", "severo", "ninguna", "ninguno"
        };

        // Regresa la alerta si la contraindicación del ejercicio coincide con algo del expediente; si no, null.
        // Coincide cuando un término contiene al otro ("rodilla" / "lesión de rodilla") o comparten una palabra
        // significativa ("lesión de rodilla" / "rodilla operada").
        public static string? RevisarContraindicacion(DataRow? expediente, string ejercicio, string? contraIndicacion)
        {
            if (expediente == null)
                return null;

            foreach (string termino in Terminos(contraIndicacion))
            {
                foreach (var (columna, etiqueta) in CamposMedicos)
                {
                    string? valor = expediente[columna] == DBNull.Value ? null : expediente[columna].ToString();

                    foreach (string dato in Terminos(valor))
                    {
                        if (Coinciden(termino, dato))
                            return $"{ejercicio}: está contraindicado por \"{termino}\" y el socio registra en {etiqueta.ToLower()}: \"{dato}\".";
                    }
                }
            }
            return null;
        }

        // Alertas de todos los ejercicios de la rutina (una por ejercicio distinto)
        public static List<string> RevisarRutina(DataRow? expediente, IEnumerable<(string Ejercicio, string? ContraIndicacion)> ejercicios)
        {
            return ejercicios
                .Select(e => RevisarContraindicacion(expediente, e.Ejercicio, e.ContraIndicacion))
                .Where(a => a != null)
                .Distinct()
                .ToList()!;
        }

        // Separa un texto en términos ("Rodilla, hipertensión / asma" -> 3 términos), sin "Ninguna"
        private static IEnumerable<string> Terminos(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Enumerable.Empty<string>();

            return texto.Replace(" y ", ",").Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => !PalabrasGenerales.Contains(Normalizar(t)) && Normalizar(t) != "n/a");
        }

        private static bool Coinciden(string a, string b)
        {
            string x = Normalizar(a), y = Normalizar(b);
            if (x.Contains(y) || y.Contains(x))
                return true;

            var palabrasX = Palabras(x);
            return Palabras(y).Any(palabrasX.Contains);
        }

        private static HashSet<string> Palabras(string texto)
        {
            return texto.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 4 && !PalabrasGenerales.Contains(p))
                .ToHashSet();
        }

        // Minúsculas y sin acentos, para comparar "Lesión" con "lesion"
        private static string Normalizar(string texto)
        {
            var sb = new StringBuilder();
            foreach (char c in texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        #endregion

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
