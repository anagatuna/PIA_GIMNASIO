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
            RolesPermitidos = new string[] { "Administrador", "Entrenador" };
        }

        // Datos del formulario (cabecera + ejercicios)
        [BindProperty]
        public bdRutina Rutina { get; set; } = new bdRutina();

        // El entrenador confirma que revisó las alertas médicas
        [BindProperty]
        public bool ConfirmarAlertas { get; set; }

        // Para los combos y el expediente
        public List<SelectListItem> Socios { get; set; } = new List<SelectListItem>();
        public List<int> SociosConExpediente { get; set; } = new List<int>();
        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Objetivos { get; set; } = new List<SelectListItem>();
        public List<DataRow> Ejercicios { get; set; } = new List<DataRow>();
        public DataRow? Expediente { get; set; }
        public Dictionary<int, string> AlertasPorEjercicio { get; set; } = new Dictionary<int, string>();
        public List<string> Alertas { get; set; } = new List<string>();

        // Al abrir el diseñador (opcionalmente con un socio ya elegido)
        public void OnGet(int? idCliente)
        {
            Rutina.IdCliente = idCliente;
            if (EsEntrenador)
                Rutina.IdEmpleado = IdEmpleadoSesion();   // El entrenador solo asigna rutinas a su nombre

            Cargar();
        }

        // Al cambiar de socio: regresa su expediente y la alerta de cada ejercicio (lo usa el JavaScript)
        public IActionResult OnGetExpediente(int idCliente)
        {
            CargarEjercicios();
            DataRow? expediente = _logica.ObtenerExpediente(idCliente);

            if (expediente == null)
                return new JsonResult(new { tieneExpediente = false, alertas = new Dictionary<int, string>() });

            return new JsonResult(new
            {
                tieneExpediente = true,
                expediente = new
                {
                    tipoSangre = expediente["TipoSangre"].ToString(),
                    padecimientos = expediente["PadecimientosCronicos"].ToString(),
                    lesiones = expediente["LesionesPrevias"].ToString(),
                    alergias = expediente["Alergias"].ToString(),
                    condicion = expediente["CondicionRiesgo"].ToString()
                },
                alertas = CalcularAlertas(expediente)
            });
        }

        // Botón Guardar rutina
        public IActionResult OnPost()
        {
            if (EsEntrenador)
                Rutina.IdEmpleado = IdEmpleadoSesion();

            Cargar();

            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                return Page();
            }

            // Verificación médica: si hay contraindicaciones, el entrenador debe confirmarlas
            foreach (bdDetalleRutina d in Rutina.Detalles)
            {
                DataRow ejercicio = BuscarEjercicio(d.IdEjercicio)!;
                string? alerta = logicaRutina.RevisarContraindicacion(Expediente, ejercicio["Descripcion"].ToString()!, ejercicio["ContraIndicacion"].ToString());

                // Una alerta por ejercicio, aunque se repita en varios días
                if (alerta != null && !Alertas.Contains(alerta))
                    Alertas.Add(alerta);
            }

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

        // Llena combos, ejercicios y el expediente del socio elegido
        private void Cargar()
        {
            Objetivos = CargarCombo(logicaCatalogo.Objetivos);
            try
            {
                DataTable socios = _logica.ConsultarSocios();
                Socios = CargarCombo(socios, "IdCliente", "Socio");
                foreach (DataRow r in socios.Rows)
                {
                    if (Convert.ToInt32(r["TieneExpediente"]) == 1)
                        SociosConExpediente.Add(Convert.ToInt32(r["IdCliente"]));
                }

                Entrenadores = CargarCombo(_logica.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");
                CargarEjercicios();

                if (Rutina.IdCliente != null)
                {
                    Expediente = _logica.ObtenerExpediente(Rutina.IdCliente.Value);
                    AlertasPorEjercicio = CalcularAlertas(Expediente);
                }
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        private void CargarEjercicios()
        {
            Ejercicios = new List<DataRow>();
            foreach (DataRow r in _logicaCatalogo.ConsultarEjercicios().Rows)
                Ejercicios.Add(r);
        }

        private DataRow? BuscarEjercicio(int idEjercicio)
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
                string? alerta = logicaRutina.RevisarContraindicacion(expediente, e["Descripcion"].ToString()!, e["ContraIndicacion"].ToString());
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
