using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Logica
{
    // Tipos de membresía (spMembresia). Es un catálogo fijo (Bronce, Plata, Oro) que se carga
    // con el script de la base; aquí solo se consulta para los combos de suscripciones y reportes.
    public class logicaMembresia
    {
        private readonly bdConexion _bd;

        public logicaMembresia(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 3: todas las membresías (combos de filtros)
        public DataTable Consultar()
        {
            SqlParameter[] p = {
                new SqlParameter("@Accion", 3),
                new SqlParameter("@Nombre", DBNull.Value),
                new SqlParameter("@IdEstatus", DBNull.Value)
            };
            return _bd.EjecutarStoredProcedure("spMembresia", p);
        }

        // Accion 6: membresías activas para vender suscripciones
        public DataTable ConsultarActivas()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spMembresia", p);
        }
    }
}
