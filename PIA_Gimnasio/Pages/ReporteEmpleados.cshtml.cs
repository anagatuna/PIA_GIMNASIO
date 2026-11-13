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
        private readonly logicaEmpleado _logicaEmpleado;

        public ReporteEmpleadosModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEmpleado = logicaEmpleado;
            RolesPermitidos = new string[] { "Administrador" };
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

        // Cantidad de empleados por sucursal: nombre de la sucursal -> total
        public Dictionary<string, int> PorSede { get; set; } = new Dictionary<string, int>();

        public void OnGet()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Puestos = CargarCombo(_logicaEmpleado.ConsultarPuestos(), "IdPuesto", "Nombre");
                Especialidades = CargarCombo(_logicaEmpleado.ConsultarEspecialidades(), "IdEspecialidad", "Nombre");

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
                    string sucursal = r["Sucursal"].ToString()!;
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
            DataTable dt = _logica.Empleados(IdSucursal, IdPuesto, IdEspecialidad, Desde, Hasta);
            return ExportarExcel(dt, "Reporte_Empleados",
                new string[] { "Sucursal", "Empleado", "Puesto", "Especialidades", "Correo", "Teléfono", "Fecha de contratación", "Estatus" },
                new string[] { "Sucursal", "Empleado", "Puesto", "Especialidades", "Correo", "Telefono", "FechaContratacion", "Estatus" });
        }
    }
}
