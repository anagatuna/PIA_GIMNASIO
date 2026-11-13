using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 4: ocupación y asistencia de clases. El entrenador solo ve sus clases.
    public class ReporteClasesModel : PaginaProtegida
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEmpleado _logicaEmpleado;

        public ReporteClasesModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEmpleado = logicaEmpleado;
            RolesPermitidos = new string[] { "Administrador", "Entrenador" };
        }

        // Filtros
        [BindProperty(SupportsGet = true)] public string? Clase { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEmpleado { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<DataRow> Filas { get; set; } = new List<DataRow>();

        // Resumen
        public int TotalReservas { get; set; }
        public double OcupacionPromedio { get; set; }
        public double? Efectividad { get; set; }

        public void OnGet()
        {
            // Por omisión, el mes actual
            if (Desde == null)
                Desde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (Hasta == null)
                Hasta = Desde.Value.AddMonths(1).AddDays(-1);

            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                if (!EsEntrenador)
                    Entrenadores = CargarCombo(_logicaEmpleado.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");

                if (Desde > Hasta)
                {
                    Mensaje = "La fecha inicial no puede ser posterior a la fecha final.";
                    TipoMensaje = "warning";
                    return;
                }

                DataTable dt = Consultar();

                double sumaOcupacion = 0;
                int asistieron = 0;
                int faltas = 0;
                foreach (DataRow r in dt.Rows)
                {
                    TotalReservas += Convert.ToInt32(r["Reservas"]);
                    sumaOcupacion += Convert.ToDouble(r["Ocupacion"]);
                    asistieron += Convert.ToInt32(r["Asistieron"]);
                    faltas += Convert.ToInt32(r["Faltas"]);
                }

                OcupacionPromedio = dt.Rows.Count == 0 ? 0 : sumaOcupacion / dt.Rows.Count;
                if (asistieron + faltas > 0)
                    Efectividad = asistieron * 100.0 / (asistieron + faltas);

                Filas = Paginar(dt);
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // Botón Exportar a Excel
        public IActionResult OnGetExportar()
        {
            return ExportarExcel(Consultar(), "Reporte_Clases",
                new string[] { "Clase", "Fecha", "Hora", "Salón", "Sucursal", "Entrenador", "Cupo", "Reservas", "Ocupación %", "Asistieron", "Faltas", "Efectividad %" },
                new string[] { "Clase", "Fecha", "HoraInicio", "Salon", "Sucursal", "Entrenador", "CupoMaximo", "Reservas", "Ocupacion", "Asistieron", "Faltas", "Efectividad" });
        }

        // El entrenador solo ve sus propias clases
        private DataTable Consultar()
        {
            if (EsEntrenador)
                IdEmpleado = IdEmpleadoSesion() ?? -1;
            return _logica.Clases(Clase, IdSucursal, IdEmpleado, Desde, Hasta);
        }
    }
}
