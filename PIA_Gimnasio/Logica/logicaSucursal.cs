using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Sucursales (spSucursal)
    public class logicaSucursal
    {
        private readonly bdConexion _bd;

        public logicaSucursal(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1 (registrar) o 4 (modificar)
        public bdRespuesta Guardar(bdSucursal s, int usuarioSesion)
        {
            int accion = s.IdSucursal == 0 ? 1 : 4;

            SqlParameter[] p = {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@IdSucursal", s.IdSucursal),
                new SqlParameter("@IdEstatus", Valor(s.IdEstatus)),
                new SqlParameter("@IdEstado", Valor(s.IdEstado)),
                new SqlParameter("@Nombre", Valor(s.Nombre?.Trim().ToUpper())),
                new SqlParameter("@Telefono", Valor(s.Telefono)),
                new SqlParameter("@AforoMaximo", Valor(s.AforoMaximo)),
                new SqlParameter("@HoraApertura", Valor(s.HoraApertura)),
                new SqlParameter("@HoraCierre", Valor(s.HoraCierre)),
                new SqlParameter("@Calle", Valor(s.Calle)),
                new SqlParameter("@NumeroInt", Valor(s.NumeroInt)),
                new SqlParameter("@Colonia", Valor(s.Colonia)),
                new SqlParameter("@Municipio", Valor(s.Municipio)),
                new SqlParameter("@CodigoPostal", Valor(s.CodigoPostal)),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSucursal", p));
        }

        // Accion 2: leer un registro
        public bdSucursal? Obtener(int id)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 2),
                new SqlParameter("@IdSucursal", id)
            };
            DataTable dt = _bd.EjecutarStoredProcedure("spSucursal", p);

            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new bdSucursal
            {
                IdSucursal = Convert.ToInt32(r["IdSucursal"]),
                IdEstatus = Convert.ToInt32(r["IdEstatus"]),
                IdEstado = Convert.ToInt32(r["IdEstado"]),
                Nombre = r["Nombre"].ToString(),
                Telefono = r["Telefono"].ToString(),
                AforoMaximo = Convert.ToInt32(r["AforoMaximo"]),
                HoraApertura = (TimeSpan)r["HoraApertura"],
                HoraCierre = (TimeSpan)r["HoraCierre"],
                Calle = r["Calle"].ToString(),
                NumeroInt = r["NumeroInt"].ToString(),
                Colonia = r["Colonia"].ToString(),
                Municipio = r["Municipio"].ToString(),
                CodigoPostal = r["CodigoPostal"].ToString()
            };
        }

        // Accion 3: consulta general (filtros opcionales)
        public DataTable Consultar(string? nombre, int? idEstado, int? idEstatus)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Nombre", Valor(nombre)),
                new SqlParameter("@IdEstado", Valor(idEstado)),
                new SqlParameter("@IdEstatus", Valor(idEstatus))
            };
            return _bd.EjecutarStoredProcedure("spSucursal", p);
        }

        // Accion 5: eliminar (baja lógica)
        public bdRespuesta Eliminar(int id, int usuarioSesion)
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 5),
                new SqlParameter("@IdSucursal", id),
                new SqlParameter("@UsuarioSesion", usuarioSesion)
            };
            return Respuesta(_bd.EjecutarStoredProcedure("spSucursal", p));
        }

        // Accion 6: sucursales activas (combos de empleados, socios, clases y control de acceso)
        public DataTable ConsultarActivas()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spSucursal", p);
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
            return string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
        }

        // Manda NULL a SQL cuando el dato (número, fecha u hora) no tiene valor
        private object Valor(object? dato)
        {
            return dato ?? DBNull.Value;
        }
    }
}
