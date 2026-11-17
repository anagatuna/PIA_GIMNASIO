using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 1: empleados y distribución por sucursal (solo Administrador)
    public class ReporteEmpleadosModel : PaginaProtegida
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaCatalogo _logicaCatalogo;

        public ReporteEmpleadosModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaCatalogo logicaCatalogo)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaCatalogo = logicaCatalogo;
            RolesPermitidos = new string[] { "ADMINISTRADOR" };
        }

        // Filtros
        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdPuesto { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEspecialidad { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Puestos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Especialidades { get; set; } = new List<SelectListItem>();
        public List<DataRow> Filas { get; set; } = new List<DataRow>();

        // Cantidad de empleados de cada sucursal (nombre de la sucursal y su total)
        public Dictionary<string, int> PorSede { get; set; } = new Dictionary<string, int>();

        public void OnGet()
        {
            // Las fechas fuera de rango se toman como vacías
            Desde = FechaValida(Desde);
            Hasta = FechaValida(Hasta);

            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Puestos = CargarCombo(_logicaCatalogo.ConsultarPuestos(), "IdCatalogo", "Descripcion");
                Especialidades = CargarCombo(_logicaCatalogo.ConsultarEspecialidades(), "IdCatalogo", "Descripcion");

                if (Desde != null && Hasta != null && Desde > Hasta)
                {
                    Mensaje = "La fecha inicial no puede ser posterior a la fecha final.";
                    TipoMensaje = "warning";
                    return;
                }

                DataTable dt = _logica.Empleados(IdSucursal, IdPuesto, IdEspecialidad, Desde, Hasta);

                // Cuenta los empleados de cada sucursal
                foreach (DataRow r in dt.Rows)
                {
                    string sucursal = r["Sucursal"].ToString() ?? "";
                    if (PorSede.ContainsKey(sucursal))
                        PorSede[sucursal]++;
                    else
                        PorSede[sucursal] = 1;
                }

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

            DataTable dt = _logica.Empleados(IdSucursal, IdPuesto, IdEspecialidad, Desde, Hasta);
            return ExportarExcel(dt, "Reporte_Empleados",
                new string[] { "Empleado", "CURP", "RFC", "Puesto", "Especialidades", "Fecha de contratación", "Correo", "Teléfono", "Sucursal" },
                new string[] { "Empleado", "CURP", "RFC", "Puesto", "Especialidades", "FechaContratacion", "Correo", "Telefono", "Sucursal" });
        }
    }
}
