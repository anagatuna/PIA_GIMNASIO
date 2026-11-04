using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 3: entradas, salidas y minutos de permanencia. Permisos: Seguridad/Modulos.cs (Administrador y Recepcionista)
    public class ReporteControlAccesoModel : PaginaBase
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;

        public ReporteControlAccesoModel(logicaReporte logica, logicaSucursal logicaSucursal)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
        }

        [BindProperty(SupportsGet = true)] public string? Busqueda { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public List<DataRow> Filas { get; set; } = new();
        public int Dentro { get; set; }
        public double PromedioMinutos { get; set; }
        public int SociosDistintos { get; set; }

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Busqueda", Busqueda), ("IdSucursal", IdSucursal),
                ("Desde", Desde?.ToString("yyyy-MM-dd")), ("Hasta", Hasta?.ToString("yyyy-MM-dd")));
        }

        private DataTable Consultar() => _logica.ControlAcceso(Busqueda, IdSucursal, Desde, Hasta);

        public void OnGet()
        {
            // Por omisión, los últimos 7 días
            Desde ??= DateTime.Today.AddDays(-6);
            Hasta ??= DateTime.Today;

            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");

                // Sin consulta si el rango de fechas está invertido
                if (!RangoFechasValido(Desde, Hasta))
                    return;

                DataTable tabla = Consultar();
                var filas = tabla.AsEnumerable().ToList();
                Dentro = filas.Count(r => r["FechaSalida"] == DBNull.Value);
                // El promedio solo considera visitas terminadas (las que siguen en curso aún no tienen permanencia final)
                var minutos = filas.Where(r => Convert.ToInt32(r["EnCurso"]) == 0).Select(r => Convert.ToInt32(r["Minutos"])).ToList();
                PromedioMinutos = minutos.Count == 0 ? 0 : minutos.Average();
                SociosDistintos = filas.Select(r => r["Folio"]).Distinct().Count();
                Filas = Paginar(tabla);
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public IActionResult OnGetExportar()
        {
            if (!RangoFechasValido(Desde, Hasta))
                return Redirigir();

            return ExportarCsv(Consultar(), "ControlAcceso",
                ("Folio", "Folio"), ("Socio", "Socio"), ("Sucursal", "Sucursal"),
                ("Entrada", "FechaEntrada"), ("Salida", "FechaSalida"), ("Minutos de permanencia", "Minutos"), ("En curso (1=Sí)", "EnCurso"));
        }
    }
}
