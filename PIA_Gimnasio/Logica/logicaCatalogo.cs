using System.Data;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Logica
{
    public class logicaCatalogo
    {
        // Listas fijas que se usan en los combos y se validan en las páginas
        public static List<string> TiposSangre = new List<string> { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
        public static List<string> DiasSemana = new List<string> { "LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO" };
        public static List<string> Objetivos = new List<string> { "HIPERTROFIA", "PERDIDA DE PESO", "REACONDICIONAMIENTO", "FUERZA", "RESISTENCIA" };
        public static List<string> GruposMusculares = new List<string> { "PECHO", "ESPALDA", "PIERNA", "GLUTEO", "HOMBRO", "BRAZO", "ABDOMEN", "CARDIO", "CUERPO COMPLETO" };
        public static List<string> NivelesDificultad = new List<string> { "PRINCIPIANTE", "INTERMEDIO", "AVANZADO" };

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

        // Accion 6: tipos de membresía activos para vender.
        // La descripción del combo se arma aquí"
        public DataTable ConsultarMembresiasVenta()
        {
            DataTable dt = Consultar(6);

            foreach (DataRow fila in dt.Rows)
            {
                decimal precio = Convert.ToDecimal(fila["Precio"]);
                fila["Descripcion"] = fila["Descripcion"] + " - $" + precio.ToString("N2")
                                      + " (" + fila["MaxClasesSemana"] + " clases/sem)";
            }
            return dt;
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
