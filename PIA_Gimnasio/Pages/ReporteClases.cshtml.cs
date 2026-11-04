using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 4: ocupación y asistencia de clases. El entrenador solo ve sus clases.
    // Permisos: Seguridad/Modulos.cs (Administrador y Entrenador)
    public class ReporteClasesModel : PaginaBase
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEmpleado _logicaEmpleado;

        public ReporteClasesModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEmpleado = logicaEmpleado;
        }

        [BindProperty(SupportsGet = true)] public string? Clase { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEmpleado { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public List<SelectListItem> Entrenadores { get; set; } = new();
        public List<DataRow> Filas { get; set; } = new();
        public int TotalReservas { get; set; }
        public double OcupacionPromedio { get; set; }
        public double? Efectividad { get; set; }

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Clase", Clase), ("IdSucursal", IdSucursal), ("IdEmpleado", IdEmpleado),
                ("Desde", Desde?.ToString("yyyy-MM-dd")), ("Hasta", Hasta?.ToString("yyyy-MM-dd")));
        }

        private DataTable Consultar()
        {
            if (EsEntrenador)
                IdEmpleado = IdEmpleadoSesion() ?? -1;
            return _logica.Clases(Clase, IdSucursal, IdEmpleado, Desde, Hasta);
        }

        public void OnGet()
        {
            // Por omisión, el mes actual
            Desde ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Hasta ??= Desde.Value.AddMonths(1).AddDays(-1);

            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                if (!EsEntrenador)
                    Entrenadores = CargarCombo(_logicaEmpleado.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");

                DataTable tabla = Consultar();
                var filas = tabla.AsEnumerable().ToList();
                TotalReservas = filas.Sum(r => Convert.ToInt32(r["Reservas"]));
                OcupacionPromedio = filas.Count == 0 ? 0 : filas.Average(r => Convert.ToDouble(r["Ocupacion"]));
                int asistieron = filas.Sum(r => Convert.ToInt32(r["Asistieron"]));
                int faltas = filas.Sum(r => Convert.ToInt32(r["Faltas"]));
                Efectividad = asistieron + faltas == 0 ? null : asistieron * 100.0 / (asistieron + faltas);
                Filas = Paginar(tabla);
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(Consultar(), "Clases",
                ("Clase", "Clase"), ("Fecha", "Fecha"), ("Hora", "HoraInicio"), ("Salón", "Salon"), ("Sucursal", "Sucursal"),
                ("Entrenador", "Entrenador"), ("Cupo", "CupoMaximo"), ("Reservas", "Reservas"), ("Ocupación %", "Ocupacion"),
                ("Asistieron", "Asistieron"), ("Faltas", "Faltas"), ("Efectividad %", "Efectividad"));
        }
    }
}
