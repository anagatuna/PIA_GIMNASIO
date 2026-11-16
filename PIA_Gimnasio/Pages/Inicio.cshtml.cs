using System.Data;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Pantalla de inicio según el rol:
    //   Administrador -> resumen de todas las sucursales (con ventas del mes y ocupación por sucursal)
    //   Recepcionista -> resumen de su sucursal
    //   Entrenador    -> su agenda de la semana
    public class InicioModel : PaginaProtegida
    {
        private readonly logicaInicio _logica;
        private readonly logicaClase _logicaClase;
        private readonly logicaSucursal _logicaSucursal;

        public InicioModel(logicaInicio logica, logicaClase logicaClase, logicaSucursal logicaSucursal)
        {
            _logica = logica;
            _logicaClase = logicaClase;
            _logicaSucursal = logicaSucursal;
        }

        public string Nombre { get; set; } = "";
        public string Rol { get; set; } = "";
        public string DescripcionRol { get; set; } = "";

        // Resumen (Administrador y Recepcionista)
        public DataRow? Indicadores { get; set; }
        public List<DataRow> PorVencer { get; set; } = new List<DataRow>();
        public List<DataRow> ClasesHoy { get; set; } = new List<DataRow>();
        public List<DataRow> Sucursales { get; set; } = new List<DataRow>();

        // Agenda de la semana (Entrenador)
        public List<DataRow> Agenda { get; set; } = new List<DataRow>();

        public static string[] Dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

        // Lunes de la semana actual
        public DateTime Lunes
        {
            get
            {
                DateTime fecha = DateTime.Today;
                while (fecha.DayOfWeek != DayOfWeek.Monday)
                    fecha = fecha.AddDays(-1);
                return fecha;
            }
        }

        public void OnGet()
        {
            Nombre = HttpContext.Session.GetString("Nombre") ?? "";
            Rol = RolSesion();

            if (Rol == "Administrador")
                DescripcionRol = "Tienes acceso total al sistema, incluida la creación de cuentas de usuario.";
            else if (Rol == "Recepcionista")
                DescripcionRol = "Registra socios, controla el acceso de tu sucursal y captura reservas de clases.";
            else if (Rol == "Entrenador")
                DescripcionRol = "Consulta expedientes médicos, diseña y asigna rutinas y pasa lista en tus clases.";

            try
            {
                if (Rol == "Entrenador")
                {
                    // Sin empleado ligado no hay agenda (-1 no coincide con ningún entrenador)
                    int idEmpleado = IdEmpleadoSesion() ?? -1;
                    foreach (DataRow r in _logicaClase.Consultar(Lunes, Lunes.AddDays(4), null, null, idEmpleado).Rows)
                        Agenda.Add(r);
                }
                else
                {
                    // El administrador ve todas las sucursales; la recepcionista, solo la suya
                    int? idSucursal = null;
                    if (!EsAdministrador)
                        idSucursal = IdSucursalSesion() ?? -1;

                    Indicadores = _logica.ObtenerIndicadores(idSucursal);

                    foreach (DataRow r in _logica.ConsultarPorVencer(idSucursal).Rows)
                        PorVencer.Add(r);

                    foreach (DataRow r in _logicaClase.Consultar(DateTime.Today, DateTime.Today, null, idSucursal, null).Rows)
                        ClasesHoy.Add(r);

                    if (EsAdministrador)
                    {
                        foreach (DataRow r in _logicaSucursal.Consultar(null, null, null).Rows)
                            Sucursales.Add(r);
                    }
                }
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
