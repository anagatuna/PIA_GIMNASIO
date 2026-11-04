using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Agenda del día y pase de lista. El entrenador solo ve sus clases; el administrador ve todas.
    // Permisos: Seguridad/Modulos.cs (Administrador y Entrenador)
    public class PaseListaModel : PaginaBase
    {
        private readonly logicaPaseLista _logica;

        public PaseListaModel(logicaPaseLista logica)
        {
            _logica = logica;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Fecha { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdClase { get; set; }

        // IdReserva -> estatus (5 Confirmado, 6 Asistió, 7 Falta). Se envía como Asistencia[IdReserva]
        [BindProperty]
        public Dictionary<int, int> Asistencia { get; set; } = new();

        public DateTime Dia => (Fecha ?? DateTime.Today).Date;
        public List<DataRow> Clases { get; set; } = new();
        public List<DataRow> Participantes { get; set; } = new();
        public DataRow? ClaseActual => Clases.FirstOrDefault(c => Convert.ToInt32(c["IdClase"]) == IdClase);

        // El entrenador queda limitado a sus propias clases
        private int? Entrenador => EsEntrenador ? IdEmpleadoSesion() ?? -1 : null;

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Fecha", Dia.ToString("yyyy-MM-dd")), ("IdClase", IdClase));
        }

        private void CargarPantalla()
        {
            try
            {
                Clases = _logica.ConsultarClases(Dia, Entrenador).AsEnumerable().ToList();

                // Si no se eligió clase, abre la primera del día
                if (!IdClase.HasValue || ClaseActual == null)
                    IdClase = Clases.Count > 0 ? Convert.ToInt32(Clases[0]["IdClase"]) : null;

                if (IdClase.HasValue)
                    Participantes = _logica.ConsultarParticipantes(IdClase.Value, Entrenador).AsEnumerable().ToList();
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public void OnGet()
        {
            CargarPantalla();
        }

        public IActionResult OnPostGuardar()
        {
            if (!IdClase.HasValue || Asistencia.Count == 0)
            {
                MostrarMensaje("No hay participantes para registrar.", "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                // El SP valida que la clase ya haya comenzado y que sea del entrenador
                MostrarRespuesta(_logica.Guardar(IdClase.Value, Entrenador, Asistencia, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el pase de lista.", "danger");
            }

            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

            return Redirigir();
        }
    }
}
