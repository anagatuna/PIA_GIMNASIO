using System.Data;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Pantalla de inicio según el rol:
    //   Administrador -> resumen de todas las sucursales (con ocupación por sucursal)
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

        public string Rol { get; set; } = "";
        public string NombreSucursal { get; set; } = "";   // Recepcionista: su sucursal

        // Resumen (Administrador y Recepcionista)
        public DataRow? Indicadores { get; set; }
        public List<DataRow> PorVencer { get; set; } = new List<DataRow>();
        public List<DataRow> ClasesHoy { get; set; } = new List<DataRow>();
        public List<DataRow> Sucursales { get; set; } = new List<DataRow>();

        // Agenda de la semana (Entrenador)
        public List<DataRow> Agenda { get; set; } = new List<DataRow>();
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
            Rol = RolSesion();

            try
            {
                if (Rol == "ENTRENADOR")
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
                    {
                        idSucursal = IdSucursalSesion() ?? -1;

                        // Nombre de su sucursal para el título del resumen
                        bdSucursal? sucursal = _logicaSucursal.Obtener(idSucursal.Value);
                        if (sucursal != null)
                            NombreSucursal = sucursal.Nombre ?? "";
                    }

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
