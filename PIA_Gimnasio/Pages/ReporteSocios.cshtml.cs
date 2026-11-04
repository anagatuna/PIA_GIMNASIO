using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 2: socios y estado de membresías. Permisos: Seguridad/Modulos.cs (Administrador y Recepcionista)
    public class ReporteSociosModel : PaginaBase
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaMembresia _logicaMembresia;

        public ReporteSociosModel(logicaReporte logica, logicaSucursal logicaSucursal, logicaMembresia logicaMembresia)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaMembresia = logicaMembresia;
        }

        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdTipoMembresia { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEstatus { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public List<SelectListItem> Membresias { get; set; } = new();

        // Estatus de la membresía (0 = nunca ha tenido)
        public List<SelectListItem> Estatus { get; } = new()
        {
            new("Activa", "1"), new("Vencida", "3"), new("Cancelada", "4"), new("Sin membresía", "0")
        };

        public List<DataRow> Filas { get; set; } = new();
        public Dictionary<int, int> Conteo { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("IdSucursal", IdSucursal), ("IdTipoMembresia", IdTipoMembresia), ("IdEstatus", IdEstatus));
        }

        private DataTable Consultar() => _logica.Socios(IdSucursal, IdTipoMembresia, IdEstatus);

        public void OnGet()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Membresias = CargarCombo(_logicaMembresia.Consultar(null, null), "IdTipoMembresia", "Nombre");

                DataTable tabla = Consultar();
                Conteo = tabla.AsEnumerable().GroupBy(r => Convert.ToInt32(r["IdEstatusMembresia"]))
                    .ToDictionary(g => g.Key, g => g.Count());
                Filas = Paginar(tabla);
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(Consultar(), "Socios",
                ("Folio", "Folio"), ("Socio", "Socio"), ("Correo", "Correo"), ("Teléfono", "Telefono"), ("Sucursal", "Sucursal"),
                ("Membresía", "Membresia"), ("Vencimiento", "FechaVencimiento"), ("Estatus", "EstatusMembresia"), ("Expediente (1=Sí)", "TieneExpediente"));
        }
    }
}
