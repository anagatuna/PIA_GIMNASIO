using System.Data;
using Microsoft.AspNetCore.Mvc;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Clases del día y pase de lista. El entrenador solo ve sus clases; el administrador ve todas.
    public class PaseListaModel : PaginaProtegida
    {
        private readonly logicaPaseLista _logica;

        public PaseListaModel(logicaPaseLista logica)
        {
            _logica = logica;
            RolesPermitidos = new string[] { "Administrador", "Entrenador" };
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Fecha { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdClase { get; set; }

        // IdReserva -> estatus (5 Confirmado, 6 Asistió, 7 Falta). Se envía como Asistencia[IdReserva]
        [BindProperty]
        public Dictionary<int, int> Asistencia { get; set; } = new Dictionary<int, int>();

        public List<DataRow> Clases { get; set; } = new List<DataRow>();
        public List<DataRow> Participantes { get; set; } = new List<DataRow>();
        public DataRow? ClaseActual { get; set; }

        public DateTime Dia
        {
            get { return (Fecha ?? DateTime.Today).Date; }
        }

        // El entrenador queda limitado a sus propias clases (-1 si no tiene empleado ligado)
        private int? Entrenador()
        {
            if (EsEntrenador)
                return IdEmpleadoSesion() ?? -1;
            return null;
        }

        public void OnGet()
        {
            Cargar();
        }

        // Botón Guardar asistencia
        public IActionResult OnPostGuardar()
        {
            if (IdClase == null || Asistencia.Count == 0)
            {
                Mensaje = "No hay participantes para registrar.";
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                // El SP valida que la clase ya haya comenzado y que sea del entrenador
                bdRespuesta r = _logica.Guardar(IdClase.Value, Entrenador(), Asistencia, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar el pase de lista.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Llena las clases del día y los participantes de la clase elegida
        private void Cargar()
        {
            try
            {
                Clases = new List<DataRow>();
                foreach (DataRow r in _logica.ConsultarClases(Dia, Entrenador()).Rows)
                    Clases.Add(r);

                // Busca la clase elegida; si no hay, abre la primera del día
                ClaseActual = null;
                foreach (DataRow c in Clases)
                {
                    if (Convert.ToInt32(c["IdClase"]) == IdClase)
                        ClaseActual = c;
                }
                if (ClaseActual == null && Clases.Count > 0)
                    ClaseActual = Clases[0];

                IdClase = ClaseActual == null ? null : Convert.ToInt32(ClaseActual["IdClase"]);

                Participantes = new List<DataRow>();
                if (IdClase != null)
                {
                    foreach (DataRow r in _logica.ConsultarParticipantes(IdClase.Value, Entrenador()).Rows)
                        Participantes.Add(r);
                }
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
