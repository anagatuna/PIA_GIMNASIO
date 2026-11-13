using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 5: rutinas asignadas y sus ejercicios. El entrenador solo ve las suyas.
    public class ReporteRutinasModel : PaginaProtegida
    {
        private readonly logicaReporte _logica;
        private readonly logicaEmpleado _logicaEmpleado;

        public ReporteRutinasModel(logicaReporte logica, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaEmpleado = logicaEmpleado;
            RolesPermitidos = new string[] { "Administrador", "Entrenador" };
        }

        // Filtros
        [BindProperty(SupportsGet = true)] public string? Socio { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEmpleado { get; set; }
        [BindProperty(SupportsGet = true)] public string? Objetivo { get; set; }
        [BindProperty(SupportsGet = true)] public string? GrupoMuscular { get; set; }
        [BindProperty(SupportsGet = true)] public string? NivelDificultad { get; set; }

        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Objetivos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Grupos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Niveles { get; set; } = new List<SelectListItem>();
        public List<DataRow> Filas { get; set; } = new List<DataRow>();

        // Resumen
        public int SociosDistintos { get; set; }
        public int TotalEjercicios { get; set; }

        // Si se filtra por grupo o nivel, la columna muestra solo los ejercicios que coinciden
        public bool FiltraEjercicios
        {
            get { return !string.IsNullOrEmpty(GrupoMuscular) || !string.IsNullOrEmpty(NivelDificultad); }
        }

        public void OnGet()
        {
            Objetivos = CargarCombo(logicaRutina.Objetivos);
            Grupos = CargarCombo(logicaEjercicio.GruposMusculares);
            Niveles = CargarCombo(logicaEjercicio.NivelesDificultad);

            try
            {
                if (!EsEntrenador)
                    Entrenadores = CargarCombo(_logicaEmpleado.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");

                DataTable dt = Consultar();

                List<string> folios = new List<string>();
                foreach (DataRow r in dt.Rows)
                {
                    TotalEjercicios += Convert.ToInt32(r["TotalEjercicios"]);

                    string folio = r["Folio"].ToString()!;
                    if (!folios.Contains(folio))
                        folios.Add(folio);
                }
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
            return ExportarExcel(Consultar(), "Reporte_Rutinas",
                new string[] { "Asignación", "Folio", "Socio", "Entrenador", "Rutina", "Objetivo", "Días", "Ejercicios", "Detalle" },
                new string[] { "FechaAsignacion", "Folio", "Socio", "Entrenador", "Rutina", "Objetivo", "TotalDias", "TotalEjercicios", "Ejercicios" });
        }

        // El entrenador solo ve sus propias rutinas
        private DataTable Consultar()
        {
            if (EsEntrenador)
                IdEmpleado = IdEmpleadoSesion() ?? -1;
            return _logica.Rutinas(Socio, IdEmpleado, Objetivo, GrupoMuscular, NivelDificultad);
        }
    }
}
