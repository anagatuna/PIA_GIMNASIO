using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Ayudas comunes de la capa de lógica: cada clase indica su SP y llama Ejecutar(accion, parámetros)
    public abstract class logicaBase
    {
        protected readonly bdConexion _bd;
        protected abstract string Procedimiento { get; }

        protected logicaBase(bdConexion bd)
        {
            _bd = bd;
        }

        // Ejecuta el SP con @Accion y los parámetros indicados: Ejecutar(3, ("@Nombre", Valor(nombre)), ...)
        protected DataTable Ejecutar(int accion, params (string Nombre, object Valor)[] parametros)
        {
            SqlParameter[] p = parametros
                .Select(x => new SqlParameter(x.Nombre, x.Valor))
                .Prepend(new SqlParameter("@Accion", accion))
                .ToArray();
            return _bd.EjecutarStoredProcedure(Procedimiento, p);
        }

        // Primer renglón o null
        protected DataRow? Renglon(int accion, params (string Nombre, object Valor)[] parametros)
        {
            DataTable dt = Ejecutar(accion, parametros);
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Convierte el Mensaje y TipoMensaje del SP en bdRespuesta
        protected bdRespuesta Respuesta(int accion, params (string Nombre, object Valor)[] parametros)
        {
            return Respuesta(Ejecutar(accion, parametros));
        }

        protected static bdRespuesta Respuesta(DataTable dt)
        {
            return new bdRespuesta
            {
                Mensaje = dt.Rows[0]["Mensaje"].ToString() ?? "",
                TipoMensaje = dt.Rows[0]["TipoMensaje"].ToString() ?? ""
            };
        }

        // Manda NULL a SQL cuando el dato no tiene valor
        protected static object Valor(string? texto) => string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
        protected static object Valor(int? numero) => numero.HasValue ? numero.Value : DBNull.Value;
        protected static object Valor(decimal? numero) => numero.HasValue ? numero.Value : DBNull.Value;
        protected static object Valor(DateTime? fecha) => fecha.HasValue ? fecha.Value.Date : DBNull.Value;
        protected static object Valor(TimeSpan? hora) => hora.HasValue ? hora.Value : DBNull.Value;

        // Texto en mayúsculas (nombres y catálogos se guardan así en todo el sistema)
        protected static object Mayusculas(string? texto) => Valor(texto?.Trim().ToUpper());

        // Lectura de columnas que pueden venir en NULL
        protected static int? Entero(DataRow r, string columna) => r[columna] == DBNull.Value ? null : Convert.ToInt32(r[columna]);
        protected static DateTime? Fecha(DataRow r, string columna) => r[columna] == DBNull.Value ? null : Convert.ToDateTime(r[columna]);
        protected static TimeSpan? Hora(DataRow r, string columna) => r[columna] == DBNull.Value ? null : (TimeSpan)r[columna];
        protected static string? Texto(DataRow r, string columna) => r[columna] == DBNull.Value ? null : r[columna].ToString();
    }
}
