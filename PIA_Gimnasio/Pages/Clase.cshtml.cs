using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Programación de clases grupales en una agenda semanal de lunes a viernes (solo Administrador)
    public class ClaseModel : PaginaProtegida
    {
        private readonly logicaClase _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEmpleado _logicaEmpleado;

        public ClaseModel(logicaClase logica, logicaSucursal logicaSucursal, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEmpleado = logicaEmpleado;
            RolesPermitidos = new string[] { "Administrador" };
        }

        [BindProperty]
        public bdClase Clase { get; set; } = new bdClase();

        // Cualquier día de la semana que se muestra (yyyy-MM-dd en la URL)
        [BindProperty(SupportsGet = true)]
        public DateTime? Semana { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdSucursal { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEmpleado { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<DataRow> Clases { get; set; } = new List<DataRow>();

        public static string[] Dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

        // Lunes de la semana que se muestra
        public DateTime Lunes
        {
            get
            {
                DateTime fecha = (Semana ?? DateTime.Today).Date;
                while (fecha.DayOfWeek != DayOfWeek.Monday)
                    fecha = fecha.AddDays(-1);
                return fecha;
            }
        }

        public void OnGet()
        {
            // Valores sugeridos para una clase nueva: siguiente día hábil de la semana mostrada
            DateTime sugerida = Lunes < DateTime.Today ? DateTime.Today : Lunes;
            while (sugerida.DayOfWeek == DayOfWeek.Saturday || sugerida.DayOfWeek == DayOfWeek.Sunday)
                sugerida = sugerida.AddDays(1);

            Clase.Fecha = sugerida;
            Clase.HoraInicio = new TimeSpan(8, 0, 0);
            Clase.HoraFin = new TimeSpan(9, 0, 0);
            Clase.IdSucursal = BusquedaIdSucursal ?? IdSucursalSesion();
            Clase.IdEmpleado = BusquedaIdEmpleado;

            Cargar();
        }

        // Clic en una clase de la agenda: la carga en el formulario
        public void OnGetEditar(int id)
        {
            try
            {
                Clase = _logica.Obtener(id) ?? new bdClase();
                Semana = Clase.Fecha;   // Muestra la semana de la clase
            }
            catch (Exception)
            {
                Mensaje = "No se pudo cargar la clase.";
                TipoMensaje = "danger";
            }
            Cargar();
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                // El SP valida horario de la sucursal, choques de salón/entrenador y cupo vs aforo
                bdRespuesta r = _logica.Guardar(Clase, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                if (r.TipoMensaje == "success")
                {
                    Semana = Clase.Fecha;
                    ModelState.Clear();
                    Clase = new bdClase { Fecha = Semana, HoraInicio = new TimeSpan(8, 0, 0), HoraFin = new TimeSpan(9, 0, 0) };
                }
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar la clase.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Cancelar clase (y sus reservas)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                bdRespuesta r = _logica.Eliminar(id, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al cancelar la clase.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Exportar a Excel (clases de la semana)
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(Lunes, Lunes.AddDays(4), null, BusquedaIdSucursal, BusquedaIdEmpleado);
            return ExportarExcel(dt, "Clases",
                new string[] { "Clase", "Fecha", "Inicio", "Fin", "Salón", "Sucursal", "Entrenador", "Cupo", "Reservadas" },
                new string[] { "Nombre", "Fecha", "HoraInicio", "HoraFin", "Salon", "Sucursal", "Entrenador", "CupoMaximo", "Reservadas" });
        }

        // Llena combos y la agenda de la semana
        private void Cargar()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Entrenadores = CargarCombo(_logicaEmpleado.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");

                DataTable dt = _logica.Consultar(Lunes, Lunes.AddDays(4), null, BusquedaIdSucursal, BusquedaIdEmpleado);
                Clases = new List<DataRow>();
                foreach (DataRow r in dt.Rows)
                    Clases.Add(r);
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // Reglas de captura. Regresa el primer error o "" si todo está bien.
        private string Validar()
        {
            if (string.IsNullOrWhiteSpace(Clase.Nombre))
                return "Ingresa el nombre de la clase.";
            if (string.IsNullOrWhiteSpace(Clase.Salon))
                return "Ingresa el salón.";
            if (Clase.Fecha == null)
                return "Ingresa la fecha de la clase.";
            if (Clase.Fecha.Value.DayOfWeek == DayOfWeek.Saturday || Clase.Fecha.Value.DayOfWeek == DayOfWeek.Sunday)
                return "Las clases grupales solo se programan de lunes a viernes.";
            if (Clase.Fecha.Value.Date < DateTime.Today)
                return "No se pueden programar clases en fechas pasadas.";
            if (Clase.HoraInicio == null || Clase.HoraFin == null)
                return "Ingresa la hora de inicio y de fin.";
            if (Clase.HoraFin <= Clase.HoraInicio)
                return "La hora de fin debe ser posterior a la hora de inicio.";
            if (Clase.CupoMaximo == null || Clase.CupoMaximo < 1 || Clase.CupoMaximo > 500)
                return "El cupo máximo debe estar entre 1 y 500.";
            if (Clase.IdSucursal == null)
                return "Selecciona la sucursal.";
            if (Clase.IdEmpleado == null)
                return "Selecciona el entrenador.";
            return "";
        }
    }
}
