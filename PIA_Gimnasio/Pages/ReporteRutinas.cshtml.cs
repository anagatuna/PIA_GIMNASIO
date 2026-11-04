using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Reporte 5: historial de rutinas prescritas. El entrenador solo ve las suyas.
    // Permisos: Seguridad/Modulos.cs (Administrador y Entrenador)
    public class ReporteRutinasModel : PaginaBase
    {
        private readonly logicaReporte _logica;
        private readonly logicaEmpleado _logicaEmpleado;

        public ReporteRutinasModel(logicaReporte logica, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaEmpleado = logicaEmpleado;
        }

        [BindProperty(SupportsGet = true)] public string? Socio { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEmpleado { get; set; }
        [BindProperty(SupportsGet = true)] public string? Objetivo { get; set; }
        [BindProperty(SupportsGet = true)] public string? GrupoMuscular { get; set; }
        [BindProperty(SupportsGet = true)] public string? NivelDificultad { get; set; }

        public List<SelectListItem> Entrenadores { get; set; } = new();
        public List<SelectListItem> Objetivos { get; } = CargarCombo(validacionRutina.Objetivos);
        public List<SelectListItem> Grupos { get; } = CargarCombo(validacionEjercicio.GruposMusculares);
        public List<SelectListItem> Niveles { get; } = CargarCombo(validacionEjercicio.NivelesDificultad);
        public List<DataRow> Filas { get; set; } = new();
        public int SociosDistintos { get; set; }
        public int TotalEjercicios { get; set; }

        // Grupo o nivel filtran los ejercicios: la columna muestra solo los que coinciden
        public bool FiltraEjercicios => !string.IsNullOrEmpty(GrupoMuscular) || !string.IsNullOrEmpty(NivelDificultad);

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Socio", Socio), ("IdEmpleado", IdEmpleado), ("Objetivo", Objetivo),
                ("GrupoMuscular", GrupoMuscular), ("NivelDificultad", NivelDificultad));
        }

        private DataTable Consultar()
        {
            if (EsEntrenador)
                IdEmpleado = IdEmpleadoSesion() ?? -1;
            return _logica.Rutinas(Socio, IdEmpleado, Objetivo, GrupoMuscular, NivelDificultad);
        }

        public void OnGet()
        {
            try
            {
                if (!EsEntrenador)
                    Entrenadores = CargarCombo(_logicaEmpleado.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");

                DataTable tabla = Consultar();
                SociosDistintos = tabla.AsEnumerable().Select(r => r["Folio"]).Distinct().Count();
                TotalEjercicios = tabla.AsEnumerable().Sum(r => Convert.ToInt32(r["TotalEjercicios"]));
                Filas = Paginar(tabla);
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(Consultar(), "Rutinas",
                ("Asignación", "FechaAsignacion"), ("Folio", "Folio"), ("Socio", "Socio"), ("Entrenador", "Entrenador"),
                ("Rutina", "Rutina"), ("Objetivo", "Objetivo"), ("Días", "TotalDias"), ("Ejercicios", "TotalEjercicios"), ("Detalle", "Ejercicios"));
        }
    }
}
