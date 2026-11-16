using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Logica
{
    // Catálogos del sistema: los combos que salen de la base (spCatalogo)
    // y las listas fijas que se validan en las páginas.
    public class logicaCatalogo
    {
        // ---------- Listas fijas ----------

        public static List<string> TiposSangre = new List<string> { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
        public static List<string> DiasSemana = new List<string> { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        public static List<string> Objetivos = new List<string> { "Hipertrofia", "Pérdida de peso", "Reacondicionamiento", "Fuerza", "Resistencia" };
        public static List<string> GruposMusculares = new List<string> { "Pecho", "Espalda", "Pierna", "Glúteo", "Hombro", "Brazo", "Abdomen", "Cardio", "Cuerpo completo" };
        public static List<string> NivelesDificultad = new List<string> { "Principiante", "Intermedio", "Avanzado" };

        // ---------- Catálogos de la base (spCatalogo) ----------

        private readonly bdConexion _bd;

        public logicaCatalogo(bdConexion bd)
        {
            _bd = bd;
        }

        // Accion 1: estados de la república (sucursales)
        public DataTable ConsultarEstados()
        {
            return Consultar(1);
        }

        // Accion 2: roles (usuarios)
        public DataTable ConsultarRoles()
        {
            return Consultar(2);
        }

        // Accion 3: puestos (empleados)
        public DataTable ConsultarPuestos()
        {
            return Consultar(3);
        }

        // Accion 4: especialidades de los entrenadores
        public DataTable ConsultarEspecialidades()
        {
            return Consultar(4);
        }

        // Accion 5: tipos de membresía (filtros)
        public DataTable ConsultarMembresias()
        {
            return Consultar(5);
        }

        // Accion 6: tipos de membresía activos para vender (con precio)
        public DataTable ConsultarMembresiasVenta()
        {
            return Consultar(6);
        }

        // Accion 7: estatus de suscripción (Activo, Vencida, Cancelada)
        public DataTable ConsultarEstatusSuscripcion()
        {
            return Consultar(7);
        }

        // Accion 8: estatus de reserva (Cancelada, Confirmada, Asistencia registrada, Falta)
        public DataTable ConsultarEstatusReserva()
        {
            return Consultar(8);
        }

        // Accion 9: ejercicios activos (diseñador de rutinas)
        public DataTable ConsultarEjercicios()
        {
            return Consultar(9);
        }

        // Ejecuta spCatalogo con la acción indicada
        private DataTable Consultar(int accion)
        {
            SqlParameter[] p = { new SqlParameter("@Accion", accion) };
            return _bd.EjecutarStoredProcedure("spCatalogo", p);
        }
    }
}
