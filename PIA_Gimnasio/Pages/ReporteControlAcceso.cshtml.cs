using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 3: entradas, salidas y minutos de permanencia (Administrador y Recepcionista)
    public class ReporteControlAccesoModel : PaginaProtegida
    {
        private readonly logicaReporte _logica;
        private readonly logicaSucursal _logicaSucursal;

        public ReporteControlAccesoModel(logicaReporte logica, logicaSucursal logicaSucursal)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            RolesPermitidos = new string[] { "ADMINISTRADOR", "RECEPCIONISTA" };
        }

        // Filtros
        [BindProperty(SupportsGet = true)] public string? Busqueda { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdSucursal { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<DataRow> Filas { get; set; } = new List<DataRow>();

        // Resumen
        public int Dentro { get; set; }
        public double PromedioMinutos { get; set; }
        public int SociosDistintos { get; set; }

        public void OnGet()
        {
            // Las fechas fuera de rango se toman como vacías
            Desde = FechaValida(Desde);
            Hasta = FechaValida(Hasta);

            // Por omisión, los últimos 7 días
            if (Desde == null)
                Desde = DateTime.Today.AddDays(-6);
            if (Hasta == null)
                Hasta = DateTime.Today;

            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");

                if (Desde > Hasta)
                {
                    Mensaje = "La fecha inicial no puede ser posterior a la fecha final.";
                    TipoMensaje = "warning";
                    return;
                }

                // La recepcionista solo consulta su sucursal
                if (EsRecepcionista)
                    IdSucursal = IdSucursalSesion() ?? -1;
                DataTable dt = _logica.ControlAcceso(Busqueda, IdSucursal, Desde, Hasta);

                int sumaMinutos = 0;
                int visitasTerminadas = 0;
                List<string> folios = new List<string>();

                foreach (DataRow r in dt.Rows)
                {
                    // Socios que siguen dentro (sin salida)
                    if (r["FechaSalida"] == DBNull.Value)
                        Dentro++;

                    // El promedio solo considera visitas terminadas
                    if (Convert.ToInt32(r["EnCurso"]) == 0)
                    {
                        sumaMinutos += Convert.ToInt32(r["Minutos"]);
                        visitasTerminadas++;
                    }

                    string folio = r["Folio"].ToString() ?? "";
                    if (!folios.Contains(folio))
                        folios.Add(folio);
                }

                PromedioMinutos = visitasTerminadas == 0 ? 0 : (double)sumaMinutos / visitasTerminadas;
                SociosDistintos = folios.Count;
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

            // La recepcionista solo consulta su sucursal
            if (EsRecepcionista)
                IdSucursal = IdSucursalSesion() ?? -1;
            DataTable dt = _logica.ControlAcceso(Busqueda, IdSucursal, Desde, Hasta);
            return ExportarExcel(dt, "Reporte_ControlAcceso",
                new string[] { "Folio", "Socio", "Sucursal", "Entrada", "Salida", "Minutos de permanencia", "En curso (1=Sí)" },
                new string[] { "Folio", "Socio", "Sucursal", "FechaEntrada", "FechaSalida", "Minutos", "EnCurso" });
        }
    }
}
