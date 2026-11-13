using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 2: socios y estado de membresías (Administrador y Recepcionista)
    public class ReporteSociosModel : PaginaProtegida
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaMembresia _logicaMembresia;

        public ReporteSociosModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaMembresia logicaMembresia)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaMembresia = logicaMembresia;
            RolesPermitidos = new string[] { "Administrador", "Recepcionista" };
        }

        // Filtros
        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdTipoMembresia { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEstatus { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Membresias { get; set; } = new List<SelectListItem>();

        // Estatus de la membresía (0 = nunca ha tenido)
        public List<SelectListItem> Estatus { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Text = "Activa", Value = "1" },
            new SelectListItem { Text = "Vencida", Value = "3" },
            new SelectListItem { Text = "Cancelada", Value = "4" },
            new SelectListItem { Text = "Sin membresía", Value = "0" }
        };

        public List<DataRow> Filas { get; set; } = new List<DataRow>();

        // Totales por estatus de la membresía
        public int Activas { get; set; }
        public int Vencidas { get; set; }
        public int Canceladas { get; set; }
        public int SinMembresia { get; set; }

        public void OnGet()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Membresias = CargarCombo(_logicaMembresia.Consultar(), "IdTipoMembresia", "Nombre");

                DataTable dt = _logica.Socios(IdSucursal, IdTipoMembresia, IdEstatus);

                foreach (DataRow r in dt.Rows)
                {
                    int estatus = Convert.ToInt32(r["IdEstatusMembresia"]);
                    if (estatus == 1) Activas++;
                    else if (estatus == 3) Vencidas++;
                    else if (estatus == 4) Canceladas++;
                    else if (estatus == 0) SinMembresia++;
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
            DataTable dt = _logica.Socios(IdSucursal, IdTipoMembresia, IdEstatus);
            return ExportarExcel(dt, "Reporte_Socios",
                new string[] { "Folio", "Socio", "Correo", "Teléfono", "Sucursal", "Membresía", "Vencimiento", "Estatus", "Expediente (1=Sí)" },
                new string[] { "Folio", "Socio", "Correo", "Telefono", "Sucursal", "Membresia", "FechaVencimiento", "EstatusMembresia", "TieneExpediente" });
        }
    }
}
