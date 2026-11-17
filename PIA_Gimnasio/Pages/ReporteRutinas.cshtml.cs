using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Reporte 5: rutinas asignadas y sus ejercicios. Solo Administrador.
    public class ReporteRutinasModel : PaginaProtegida
    {
        private readonly logicaReporte _logica;
        private readonly logicaEmpleado _logicaEmpleado;

        public ReporteRutinasModel(logicaReporte logica, logicaEmpleado logicaEmpleado)
        {
            _logica = logica;
            _logicaEmpleado = logicaEmpleado;
            RolesPermitidos = new string[] { "ADMINISTRADOR" };
        }

        // Filtros
        [BindProperty(SupportsGet = true)] public string? Socio { get; set; }
        [BindProperty(SupportsGet = true)] public int? IdEmpleado { get; set; }
        [BindProperty(SupportsGet = true)] public string? Objetivo { get; set; }
        [BindProperty(SupportsGet = true)] public string? GrupoMuscular { get; set; }
        [BindProperty(SupportsGet = true)] public string? NivelDificultad { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }     // Periodo de asignación
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Objetivos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Grupos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Niveles { get; set; } = new List<SelectListItem>();
        public List<DataRow> Filas { get; set; } = new List<DataRow>();      // Rutinas de la página actual
        public List<DataRow> Detalle { get; set; } = new List<DataRow>();    // Ejercicios de todas las rutinas encontradas

        // Resumen
        public int SociosDistintos { get; set; }
        public int TotalEjercicios { get; set; }

        // Si se filtra por grupo o nivel, cada rutina muestra solo los ejercicios que coinciden
        public bool FiltraEjercicios
        {
            get { return !string.IsNullOrEmpty(GrupoMuscular) || !string.IsNullOrEmpty(NivelDificultad); }
        }

        public void OnGet()
        {
            // Las fechas fuera de rango se toman como vacías
            Desde = FechaValida(Desde);
            Hasta = FechaValida(Hasta);

            Objetivos = CargarCombo(logicaCatalogo.Objetivos);
            Grupos = CargarCombo(logicaCatalogo.GruposMusculares);
            Niveles = CargarCombo(logicaCatalogo.NivelesDificultad);

            if (Desde != null && Hasta != null && Desde > Hasta)
            {
                Mensaje = "La fecha inicial del periodo no puede ser mayor a la final.";
                TipoMensaje = "warning";
                return;
            }

            try
            {
                Entrenadores = CargarCombo(_logicaEmpleado.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");

                DataTable dt = Consultar();

                List<string> folios = new List<string>();
                foreach (DataRow r in dt.Rows)
                {
                    TotalEjercicios += Convert.ToInt32(r["TotalEjercicios"]);

                    string folio = r["Folio"].ToString() ?? "";
                    if (!folios.Contains(folio))
                        folios.Add(folio);
                }
                SociosDistintos = folios.Count;

                Filas = Paginar(dt);

                foreach (DataRow r in ConsultarDetalle().Rows)
                    Detalle.Add(r);
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // Ejercicios de una rutina (los toma de Detalle)
        public List<DataRow> EjerciciosDe(object idRutina)
        {
            List<DataRow> lista = new List<DataRow>();
            foreach (DataRow d in Detalle)
            {
                if (d["IdRutina"].ToString() == idRutina.ToString())
                    lista.Add(d);
            }
            return lista;
        }

        // Botón Exportar a Excel: un renglón por ejercicio con los datos de su rutina
        public IActionResult OnGetExportar()
        {
            Desde = FechaValida(Desde);
            Hasta = FechaValida(Hasta);

            return ExportarExcel(ConsultarDetalle(), "Reporte_Rutinas",
                new string[] { "Asignación", "Folio", "Socio", "Entrenador", "Rutina", "Objetivo", "Ejercicio", "Grupo muscular", "Dificultad", "Día", "Series", "Repeticiones", "Descanso (seg)" },
                new string[] { "FechaAsignacion", "Folio", "Socio", "Entrenador", "Rutina", "Objetivo", "Ejercicio", "GrupoMuscular", "NivelDificultad", "DiaSemana", "Series", "Repeticiones", "TiempoDescanso" });
        }

        private DataTable Consultar()
        {
            return _logica.Rutinas(Socio, IdEmpleado, Objetivo, GrupoMuscular, NivelDificultad, Desde, Hasta);
        }

        private DataTable ConsultarDetalle()
        {
            return _logica.RutinasDetalle(Socio, IdEmpleado, Objetivo, GrupoMuscular, NivelDificultad, Desde, Hasta);
        }
    }
}
