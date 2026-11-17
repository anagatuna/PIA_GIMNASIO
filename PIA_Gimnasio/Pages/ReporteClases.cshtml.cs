using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 4: clases grupales, reservas y asistencia. Solo Administrador.
    public class ReporteClasesModel : PaginaProtegida
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEmpleado _logicaEmpleado;
        private readonly logicaCatalogo _logicaCatalogo;

        public ReporteClasesModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaEmpleado logicaEmpleado, logicaCatalogo logicaCatalogo)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEmpleado = logicaEmpleado;
            _logicaCatalogo = logicaCatalogo;
            RolesPermitidos = new string[] { "ADMINISTRADOR" };
        }

        // Filtros
        [BindProperty(SupportsGet = true)] public string? Clase { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEmpleado { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEstatus { get; set; }      // Estatus de reserva

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EstatusReserva { get; set; } = new List<SelectListItem>();
        public List<DataRow> Filas { get; set; } = new List<DataRow>();

        // Resumen
        public int TotalReservas { get; set; }      // Las que ocupan lugar (confirmada, asistió, falta)
        public int TotalCanceladas { get; set; }
        public double OcupacionPromedio { get; set; }
        public double? Efectividad { get; set; }

        public void OnGet()
        {
            // Las fechas fuera de rango se toman como vacías
            Desde = FechaValida(Desde);
            Hasta = FechaValida(Hasta);

            // Por omisión, el mes actual
            if (Desde == null)
                Desde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (Hasta == null)
                Hasta = Desde.Value.AddMonths(1).AddDays(-1);

            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                EstatusReserva = CargarCombo(_logicaCatalogo.ConsultarEstatusReserva(), "IdCatalogo", "Descripcion");
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
                    TotalCanceladas += Convert.ToInt32(r["Canceladas"]);
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
            Desde = FechaValida(Desde);
            Hasta = FechaValida(Hasta);

            return ExportarExcel(Consultar(), "Reporte_Clases",
                new string[] { "Clase", "Sucursal", "Fecha", "Inicio", "Fin", "Salón", "Entrenador", "Cupo", "Reservas hechas", "Confirmadas", "Canceladas", "Asistieron", "Faltas", "Ocupación %", "Efectividad %" },
                new string[] { "Clase", "Sucursal", "Fecha", "HoraInicio", "HoraFin", "Salon", "Entrenador", "CupoMaximo", "TotalReservas", "Confirmadas", "Canceladas", "Asistieron", "Faltas", "Ocupacion", "Efectividad" });
        }

        private DataTable Consultar()
        {
            return _logica.Clases(Clase, IdSucursal, IdEmpleado, Desde, Hasta, IdEstatus);
        }
    }
}
