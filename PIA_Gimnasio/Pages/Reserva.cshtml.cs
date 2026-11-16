using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Captura de reservas: valida cupo del salón y límite semanal del plan (Bronce 0, Plata 3, Oro 5).
    // Administrador y Recepcionista.
    public class ReservaModel : PaginaProtegida
    {
        private readonly logicaReserva _logica;
        private readonly logicaSocio _logicaSocio;
        private readonly logicaCatalogo _logicaCatalogo;

        public ReservaModel(logicaReserva logica, logicaSocio logicaSocio, logicaCatalogo logicaCatalogo)
        {
            _logica = logica;
            _logicaSocio = logicaSocio;
            _logicaCatalogo = logicaCatalogo;
            RolesPermitidos = new string[] { "Administrador", "Recepcionista" };
        }

        [BindProperty]
        public bdReserva Reserva { get; set; } = new bdReserva();

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? BusquedaFecha { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Socios { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Clases { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Estatus { get; set; } = new List<SelectListItem>();
        public List<DataRow> Reservas { get; set; } = new List<DataRow>();

        public void OnGet()
        {
            Cargar();
        }

        // Plan del socio y clases usadas en la semana de la clase elegida (lo usa el JavaScript)
        public IActionResult OnGetResumen(int idCliente, int? idClase)
        {
            try
            {
                DataRow? r = _logica.ObtenerResumenSocio(idCliente, idClase);
                if (r == null || r["Membresia"] == DBNull.Value)
                    return new JsonResult(new { tienePlan = false });

                return new JsonResult(new
                {
                    tienePlan = true,
                    membresia = r["Membresia"].ToString(),
                    maximo = Convert.ToInt32(r["MaxClasesSemana"]),
                    usadas = Convert.ToInt32(r["Usadas"]),
                    lunes = Convert.ToDateTime(r["Lunes"]).ToString("dd/MM/yyyy"),
                    vence = Convert.ToDateTime(r["FechaVencimiento"]).ToString("dd/MM/yyyy")
                });
            }
            catch (Exception)
            {
                return StatusCode(503);
            }
        }

        // Botón Reservar
        public IActionResult OnPostGuardar()
        {
            // La reserva guarda al empleado que la captura
            int? idEmpleado = IdEmpleadoSesion();

            if (Reserva.IdCliente == null)
                Mensaje = "Selecciona el socio.";
            else if (Reserva.IdClase == null)
                Mensaje = "Selecciona la clase.";
            else if (idEmpleado == null)
                Mensaje = "Tu cuenta no está ligada a un empleado; no puedes capturar reservas.";

            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                // El SP valida cupo, membresía vigente y límite semanal
                bdRespuesta r = _logica.Registrar(Reserva, idEmpleado!.Value, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                if (r.TipoMensaje == "success")
                {
                    ModelState.Clear();
                    Reserva = new bdReserva();
                }
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al registrar la reserva.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Cancelar reserva (usa la ventana de confirmación)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                bdRespuesta r = _logica.Cancelar(id, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al cancelar la reserva.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Exportar a Excel
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(Busqueda, null, BusquedaFecha, BusquedaIdEstatus);
            return ExportarExcel(dt, "Reservas",
                new string[] { "Folio", "Socio", "Clase", "Fecha", "Hora", "Salón", "Sucursal", "Solicitud", "Estatus" },
                new string[] { "Folio", "Socio", "Clase", "Fecha", "HoraInicio", "Salon", "Sucursal", "FechaSolicitud", "Estatus" });
        }

        // Llena combos y tabla
        private void Cargar()
        {
            try
            {
                Socios = CargarCombo(_logicaSocio.ConsultarActivos(), "IdCliente", "Socio");
                Clases = CargarCombo(_logica.ConsultarClasesDisponibles(), "IdClase", "Clase");
                Estatus = CargarCombo(_logicaCatalogo.ConsultarEstatusReserva(), "IdCatalogo", "Descripcion");
                Reservas = Paginar(_logica.Consultar(Busqueda, null, BusquedaFecha, BusquedaIdEstatus));
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
