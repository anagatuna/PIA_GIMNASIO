using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 1: empleados y distribución por sede. Permisos: Seguridad/Modulos.cs (Administrador)
    public class ReporteEmpleadosModel : PaginaBase
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEmpleado _logicaEmpleado;

        public ReporteEmpleadosModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEmpleado = logicaEmpleado;
        }

        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdPuesto { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEspecialidad { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public List<SelectListItem> Puestos { get; set; } = new();
        public List<SelectListItem> Especialidades { get; set; } = new();
        public List<DataRow> Filas { get; set; } = new();
        public List<(string Sucursal, int Total)> PorSede { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("IdSucursal", IdSucursal), ("IdPuesto", IdPuesto), ("IdEspecialidad", IdEspecialidad),
                ("Desde", Desde?.ToString("yyyy-MM-dd")), ("Hasta", Hasta?.ToString("yyyy-MM-dd")));
        }

        private DataTable Consultar() => _logica.Empleados(IdSucursal, IdPuesto, IdEspecialidad, Desde, Hasta);

        public void OnGet()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Puestos = CargarCombo(_logicaEmpleado.ConsultarPuestos(), "IdPuesto", "Nombre");
                Especialidades = CargarCombo(_logicaEmpleado.ConsultarEspecialidades(), "IdEspecialidad", "Nombre");

                DataTable tabla = Consultar();
                PorSede = tabla.AsEnumerable().GroupBy(r => r["Sucursal"].ToString()!)
                    .Select(g => (g.Key, g.Count())).ToList();
                Filas = Paginar(tabla);
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(Consultar(), "Empleados",
                ("Sucursal", "Sucursal"), ("Empleado", "Empleado"), ("Puesto", "Puesto"), ("Especialidades", "Especialidades"),
                ("Correo", "Correo"), ("Teléfono", "Telefono"), ("Fecha de contratación", "FechaContratacion"), ("Estatus", "Estatus"));
        }
    }
}
