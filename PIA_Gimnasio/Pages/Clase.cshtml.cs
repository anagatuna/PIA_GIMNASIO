using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Programación de clases grupales en una agenda semanal de lunes a viernes.
    // Permisos: Seguridad/Modulos.cs
    public class ClaseModel : PaginaBase
    {
        private readonly logicaClase _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEmpleado _logicaEmpleado;

        public ClaseModel(logicaClase logica, logicaSucursal logicaSucursal, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEmpleado = logicaEmpleado;
        }

        [BindProperty]
        public bdClase Clase { get; set; } = new bdClase();

        // Lunes de la semana que se muestra (yyyy-MM-dd en la URL)
        [BindProperty(SupportsGet = true)]
        public DateTime? Semana { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdSucursal { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEmpleado { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public List<SelectListItem> Entrenadores { get; set; } = new();
        public List<DataRow> Clases { get; set; } = new();

        public DateTime Lunes => InicioSemana(Semana ?? DateTime.Today);
        public static readonly string[] Dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

        public static DateTime InicioSemana(DateTime fecha)
        {
            int desfase = ((int)fecha.DayOfWeek + 6) % 7;   // lunes = 0
            return fecha.Date.AddDays(-desfase);
        }

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Semana", Lunes.ToString("yyyy-MM-dd")),
                ("BusquedaIdSucursal", BusquedaIdSucursal), ("BusquedaIdEmpleado", BusquedaIdEmpleado));
        }

        private void CargarPantalla()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Entrenadores = CargarCombo(_logicaEmpleado.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");
                Clases = _logica.Consultar(Lunes, Lunes.AddDays(4), null, BusquedaIdSucursal, BusquedaIdEmpleado).AsEnumerable().ToList();
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public void OnGet()
        {
            // Valores sugeridos para una clase nueva: siguiente día hábil de la semana mostrada
            DateTime sugerida = Lunes < DateTime.Today ? DateTime.Today : Lunes;
            while (sugerida.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                sugerida = sugerida.AddDays(1);

            Clase.Fecha = sugerida;
            Clase.HoraInicio = new TimeSpan(8, 0, 0);
            Clase.HoraFin = new TimeSpan(9, 0, 0);
            Clase.IdSucursal = BusquedaIdSucursal ?? (int.TryParse(User.FindFirst("IdSucursal")?.Value, out int s) ? s : null);
            Clase.IdEmpleado = BusquedaIdEmpleado;

            CargarPantalla();
        }

        public void OnGetEditar(int id)
        {
            bdClase? clase = _logica.Obtener(id);
            if (clase != null)
            {
                Clase = clase;
                Semana = clase.Fecha;   // Muestra la semana de la clase
            }
            else
                MostrarMensaje("No se encontró la clase seleccionada.", "warning");

            CargarPantalla();
        }

        public IActionResult OnPostGuardar()
        {
            List<string> errores = validacionOperacion.ValidarClase(Clase);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                // El SP valida horario de la sucursal, choques de salón/entrenador y cupo vs aforo
                MostrarRespuesta(_logica.Guardar(Clase, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar la clase.", "danger");
            }

            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

            Semana = Clase.Fecha;
            return Redirigir();
        }

        // Cancelar clase (y sus reservas)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                MostrarRespuesta(_logica.Eliminar(id, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al cancelar la clase.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(Lunes, Lunes.AddDays(4), null, BusquedaIdSucursal, BusquedaIdEmpleado), "Clases",
                ("Clase", "Nombre"), ("Fecha", "Fecha"), ("Inicio", "HoraInicio"), ("Fin", "HoraFin"), ("Salón", "Salon"),
                ("Sucursal", "Sucursal"), ("Entrenador", "Entrenador"), ("Cupo", "CupoMaximo"), ("Reservadas", "Reservadas"));
        }
    }
}
