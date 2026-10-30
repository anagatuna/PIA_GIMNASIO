using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Permisos: Seguridad/Modulos.cs
    public class RutinaNuevaModel : PaginaBase
    {
        private readonly logicaRutina _logica;
        private readonly logicaEjercicio _logicaEjercicio;

        public RutinaNuevaModel(logicaRutina logica, logicaEjercicio logicaEjercicio)
        {
            _logica = logica;
            _logicaEjercicio = logicaEjercicio;
        }

        // Datos del formulario (cabecera + matriz semanal)
        [BindProperty]
        public bdRutina Rutina { get; set; } = new bdRutina();

        // El entrenador confirma que revisó las alertas médicas
        [BindProperty]
        public bool ConfirmarAlertas { get; set; }

        // Para los combos y el expediente
        public List<SelectListItem> Socios { get; set; } = new();
        public HashSet<int> SociosConExpediente { get; set; } = new();
        public List<SelectListItem> Entrenadores { get; set; } = new();
        public List<SelectListItem> Objetivos { get; } = CargarCombo(validacionRutina.Objetivos);
        public List<DataRow> Ejercicios { get; set; } = new();
        public DataRow? Expediente { get; set; }
        public Dictionary<int, string> AlertasPorEjercicio { get; set; } = new();
        public List<string> Alertas { get; set; } = new();

        // Alerta de cada ejercicio activo para el expediente del socio (vacío si no hay expediente)
        private Dictionary<int, string> CalcularAlertas(DataRow? expediente)
        {
            var alertas = new Dictionary<int, string>();
            foreach (DataRow e in Ejercicios)
            {
                string? alerta = logicaRutina.RevisarContraindicacion(expediente, e["Nombre"].ToString()!, e["ContraIndicacion"].ToString());
                if (alerta != null)
                    alertas[Convert.ToInt32(e["IdEjercicio"])] = alerta;
            }
            return alertas;
        }

        private void CargarPantalla()
        {
            try
            {
                DataTable socios = _logica.ConsultarSocios();
                Socios = CargarCombo(socios, "IdCliente", "Socio");
                SociosConExpediente = socios.AsEnumerable().Where(r => Convert.ToInt32(r["TieneExpediente"]) == 1)
                    .Select(r => Convert.ToInt32(r["IdCliente"])).ToHashSet();

                Entrenadores = CargarCombo(_logica.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");
                Ejercicios = _logicaEjercicio.ConsultarActivos().AsEnumerable().ToList();

                // Expediente y alertas del socio elegido
                if (Rutina.IdCliente.HasValue)
                {
                    Expediente = _logica.ObtenerExpediente(Rutina.IdCliente.Value);
                    AlertasPorEjercicio = CalcularAlertas(Expediente);
                }
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        // Al abrir el diseñador (opcionalmente con un socio ya elegido)
        public void OnGet(int? idCliente)
        {
            Rutina.IdCliente = idCliente;
            if (EsEntrenador)
                Rutina.IdEmpleado = IdEmpleadoSesion();   // El entrenador solo asigna rutinas a su nombre

            CargarPantalla();
        }

        // Al cambiar de socio: regresa su expediente y la alerta de cada ejercicio (lo usa el JavaScript)
        public IActionResult OnGetExpediente(int idCliente)
        {
            Ejercicios = _logicaEjercicio.ConsultarActivos().AsEnumerable().ToList();
            DataRow? expediente = _logica.ObtenerExpediente(idCliente);

            return new JsonResult(new
            {
                tieneExpediente = expediente != null,
                expediente = expediente == null ? null : new
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

            CargarPantalla();

            // Cabecera, matriz semanal y métricas (Logica/Validacion/validacionRutina.cs)
            var idsActivos = Ejercicios.Select(e => Convert.ToInt32(e["IdEjercicio"])).ToHashSet();
            List<string> errores = validacionRutina.Validar(Rutina, idsActivos);

            // Expediente médico obligatorio
            if (Rutina.IdCliente.HasValue && Expediente == null)
                errores.Add("El socio no tiene expediente médico. Regístralo antes de asignarle una rutina.");

            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                return Page();
            }

            // Verificación médica: si hay contraindicaciones, el entrenador debe confirmarlas
            var nombres = Ejercicios.ToDictionary(e => Convert.ToInt32(e["IdEjercicio"]), e => e);
            Alertas = logicaRutina.RevisarRutina(Expediente, Rutina.Detalles
                .Select(d => (nombres[d.IdEjercicio]["Nombre"].ToString()!, nombres[d.IdEjercicio]["ContraIndicacion"].ToString())));

            if (Alertas.Count > 0 && !ConfirmarAlertas)
            {
                MostrarMensaje("Hay ejercicios contraindicados para este socio. Revisa las alertas y confirma para guardar.", "warning");
                return Page();
            }

            try
            {
                var (respuesta, idRutina) = _logica.Guardar(Rutina, IdUsuarioSesion());
                MostrarRespuesta(respuesta);

                if (respuesta.TipoMensaje != "success")
                    return Page();

                return Redirigir("/RutinaDetalle", new { id = idRutina });
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar la rutina.", "danger");
                return Page();
            }
        }
    }
}
