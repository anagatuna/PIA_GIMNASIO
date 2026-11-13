using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Logica
{
    // Catálogo de ejercicios (spEjercicio). Es fijo: se carga con el script de la base
    // y el diseñador de rutinas solo lo consulta.
    public class logicaEjercicio
    {
        // Valores permitidos del catálogo (filtros del reporte de rutinas)
        public static List<string> GruposMusculares = new List<string> { "Pecho", "Espalda", "Pierna", "Glúteo", "Hombro", "Brazo", "Abdomen", "Cardio", "Cuerpo completo" };
        public static List<string> NivelesDificultad = new List<string> { "Principiante", "Intermedio", "Avanzado" };

        private readonly bdConexion _bd;

        public logicaEjercicio(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 6: ejercicios activos (para el diseñador de rutinas)
        public DataTable ConsultarActivos()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 6) };
            return _bd.EjecutarStoredProcedure("spEjercicio", p);
        }

        // Accion 7: combo de estatus (Activo / Inactivo; lo usan Empleados, Socios y Sucursales)
        public DataTable ConsultarEstatus()
        {
            SqlParameter[] p = { new SqlParameter("@Accion", 7) };
            return _bd.EjecutarStoredProcedure("spEjercicio", p);
        }
    }
}
