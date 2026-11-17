using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Diseñador de rutinas: cabecera + ejercicios por día, con verificación del expediente médico
    public class RutinaNuevaModel : PaginaProtegida
    {
        private readonly logicaRutina _logica;
        private readonly logicaCatalogo _logicaCatalogo;

        public RutinaNuevaModel(logicaRutina logica, logicaCatalogo logicaCatalogo)
        {
            _logica = logica;
            _logicaCatalogo = logicaCatalogo;
            RolesPermitidos = new string[] { "ADMINISTRADOR", "ENTRENADOR" };
        }

        // Datos del formulario (cabecera + ejercicios)
        [BindProperty]
        public bdRutina Rutina { get; set; } = new bdRutina();

        // El entrenador confirma que revisó las alertas médicas
        [BindProperty]
        public bool ConfirmarAlertas { get; set; }

        // Para los combos y el expediente
        public List<SelectListItem> Socios { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Objetivos { get; set; } = new List<SelectListItem>();
        public List<DataRow> Ejercicios { get; set; } = new List<DataRow>();
        public DataRow? Expediente { get; set; }
        public Dictionary<int, string> AlertasPorEjercicio { get; set; } = new Dictionary<int, string>();
        public List<string> Alertas { get; set; } = new List<string>();

        // Ejercicio que se va a agregar a la matriz (fila "Agregar al día")
        [BindProperty] public string? NuevoDia { get; set; }
        [BindProperty] public int? NuevoIdEjercicio { get; set; }
        [BindProperty] public int? NuevoSeries { get; set; } = 3;
        [BindProperty] public int? NuevoRepeticiones { get; set; } = 12;
        [BindProperty] public int? NuevoDescanso { get; set; } = 60;

        // Al abrir el diseñador (opcionalmente con un socio ya elegido)
        public void OnGet(int? idCliente)
        {
            Rutina.IdCliente = idCliente;
            Cargar();
        }

        // Al cambiar de socio se recarga la página con su expediente y sus alertas
        public IActionResult OnPostSocio()
        {
            Cargar();
            return Page();
        }

        // Botón "Agregar al día": valida y agrega el ejercicio a la rutina
        public IActionResult OnPostAgregar()
        {
            Cargar();

            if (NuevoDia == null || !logicaCatalogo.DiasSemana.Contains(NuevoDia))
                Mensaje = "Selecciona el día.";
            else if (NuevoIdEjercicio == null || BuscarEjercicio(NuevoIdEjercicio.Value) == null)
                Mensaje = "Selecciona un ejercicio.";
            else if (NuevoSeries == null || NuevoSeries <= 0)
                Mensaje = "Las series deben ser mayores a 0.";
            else if (NuevoRepeticiones == null || NuevoRepeticiones <= 0)
                Mensaje = "Las repeticiones deben ser mayores a 0.";
            else if (NuevoDescanso == null || NuevoDescanso < 0)
                Mensaje = "El descanso no puede ser negativo.";

            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                return Page();
            }

            bdDetalleRutina d = new bdDetalleRutina();
            d.DiaSemana = NuevoDia;
            d.IdEjercicio = Convert.ToInt32(NuevoIdEjercicio);
            d.Series = Convert.ToInt32(NuevoSeries);
            d.Repeticiones = Convert.ToInt32(NuevoRepeticiones);
            d.TiempoDescanso = Convert.ToInt32(NuevoDescanso);
            Rutina.Detalles.Add(d);

            BuscarAlertas();
            return Page();
        }

        // Botón X de cada ejercicio: lo quita de la rutina
        public IActionResult OnPostQuitar(int indice)
        {
            if (indice >= 0 && indice < Rutina.Detalles.Count)
                Rutina.Detalles.RemoveAt(indice);

            Cargar();
            return Page();
        }

        // Botón Guardar rutina
        public IActionResult OnPost()
        {
            Cargar();

            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                return Page();
            }

            // Verificación médica: si hay contraindicaciones, el entrenador debe confirmarlas
            if (Alertas.Count > 0 && !ConfirmarAlertas)
            {
                Mensaje = "Hay ejercicios contraindicados para este socio. Revisa las alertas y confirma para guardar.";
                TipoMensaje = "warning";
                return Page();
            }

            try
            {
                bdRespuesta r = _logica.Guardar(Rutina, IdUsuarioSesion());
                if (r.TipoMensaje != "success")
                {
                    Mensaje = r.Mensaje;
                    TipoMensaje = r.TipoMensaje;
                    return Page();
                }

                // Abre el detalle de la rutina recién guardada
                return RedirectToPage("/RutinaDetalle", new { id = r.Id, guardada = true });
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar la rutina.";
                TipoMensaje = "danger";
                return Page();
            }
        }

        // Llena combos, ejercicios, el expediente del socio elegido y sus alertas
        private void Cargar()
        {
            // Fecha fuera de rango = vacía (la validación pide capturarla)
            Rutina.FechaAsignacion = FechaValida(Rutina.FechaAsignacion);

            // El entrenador solo asigna rutinas a su nombre
            if (EsEntrenador)
                Rutina.IdEmpleado = IdEmpleadoSesion();

            Objetivos = CargarCombo(logicaCatalogo.Objetivos);
            try
            {
                Socios = CargarCombo(_logica.ConsultarSocios(), "IdCliente", "Socio");
                Entrenadores = CargarCombo(_logica.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");
                CargarEjercicios();

                if (Rutina.IdCliente != null)
                {
                    Expediente = _logica.ObtenerExpediente(Rutina.IdCliente.Value);
                    AlertasPorEjercicio = CalcularAlertas(Expediente);
                }
                BuscarAlertas();
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // Alertas de los ejercicios que ya están en la rutina (una por ejercicio, aunque se repita en varios días)
        private void BuscarAlertas()
        {
            Alertas = new List<string>();
            foreach (bdDetalleRutina d in Rutina.Detalles)
            {
                if (AlertasPorEjercicio.ContainsKey(d.IdEjercicio))
                {
                    string alerta = AlertasPorEjercicio[d.IdEjercicio];
                    if (!Alertas.Contains(alerta))
                        Alertas.Add(alerta);
                }
            }
        }

        private void CargarEjercicios()
        {
            Ejercicios = new List<DataRow>();
            foreach (DataRow r in _logicaCatalogo.ConsultarEjercicios().Rows)
                Ejercicios.Add(r);
        }

        // Busca un ejercicio activo por Id (null si no existe). La página lo usa para pintar la matriz.
        public DataRow? BuscarEjercicio(int idEjercicio)
        {
            foreach (DataRow e in Ejercicios)
            {
                if (Convert.ToInt32(e["IdCatalogo"]) == idEjercicio)
                    return e;
            }
            return null;
        }

        // Alerta de cada ejercicio activo para el expediente del socio
        private Dictionary<int, string> CalcularAlertas(DataRow? expediente)
        {
            Dictionary<int, string> alertas = new Dictionary<int, string>();
            foreach (DataRow e in Ejercicios)
            {
                string? alerta = logicaRutina.RevisarContraindicacion(expediente, e["Descripcion"].ToString() ?? "", e["ContraIndicacion"].ToString());
                if (alerta != null)
                    alertas[Convert.ToInt32(e["IdCatalogo"])] = alerta;
            }
            return alertas;
        }

        // Reglas de captura. Regresa el primer error o "" si todo está bien.
        private string Validar()
        {
            if (Rutina.IdCliente == null)
                return "Selecciona el socio.";
            if (Expediente == null)
                return "El socio no tiene expediente médico. Regístralo antes de asignarle una rutina.";
            if (Rutina.IdEmpleado == null)
                return "Selecciona el entrenador.";
            if (string.IsNullOrWhiteSpace(Rutina.Nombre))
                return "Ingresa el nombre de la rutina.";
            if (string.IsNullOrEmpty(Rutina.Objetivo) || !logicaCatalogo.Objetivos.Contains(Rutina.Objetivo))
                return "Selecciona el objetivo.";
            if (Rutina.FechaAsignacion == null)
                return "Selecciona la fecha de asignación.";
            if (Rutina.Detalles.Count == 0)
                return "Agrega al menos un ejercicio a la rutina.";

            foreach (bdDetalleRutina d in Rutina.Detalles)
            {
                if (d.DiaSemana == null || !logicaCatalogo.DiasSemana.Contains(d.DiaSemana))
                    return "\"" + d.DiaSemana + "\" no es un día válido. Usa de Lunes a Domingo.";
                if (d.Series <= 0)
                    return "Las series deben ser mayores a 0.";
                if (d.Repeticiones <= 0)
                    return "Las repeticiones deben ser mayores a 0.";
                if (d.TiempoDescanso < 0)
                    return "El tiempo de descanso no puede ser negativo.";
                if (BuscarEjercicio(d.IdEjercicio) == null)
                    return "Hay ejercicios que no existen o están inactivos.";
            }
            return "";
        }
    }
}
