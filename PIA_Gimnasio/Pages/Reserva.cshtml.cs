using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Captura de reservas: valida cupo del salón y límite semanal del plan (Bronce 0, Plata 3, Oro 5).
    // Permisos: Seguridad/Modulos.cs (Administrador y Recepcionista)
    public class ReservaModel : PaginaBase
    {
        private readonly logicaReserva _logica;
        private readonly logicaSocio _logicaSocio;

        public ReservaModel(logicaReserva logica, logicaSocio logicaSocio)
        {
            _logica = logica;
            _logicaSocio = logicaSocio;
        }

        [BindProperty]
        public bdReserva Reserva { get; set; } = new bdReserva();

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? BusquedaFecha { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Socios { get; set; } = new();
        public List<SelectListItem> Clases { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<DataRow> Reservas { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Busqueda", Busqueda), ("BusquedaFecha", BusquedaFecha?.ToString("yyyy-MM-dd")), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        private void CargarPantalla()
        {
            try
            {
                Socios = CargarCombo(_logicaSocio.ConsultarActivos(), "IdCliente", "Socio");
                Clases = CargarCombo(_logica.ConsultarClasesDisponibles(), "IdClase", "Clase");
                Estatus = CargarCombo(_logica.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Reservas = Paginar(_logica.Consultar(Busqueda, null, BusquedaFecha, BusquedaIdEstatus));
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

        // Plan del socio y clases usadas en la semana (lo usa el JavaScript al elegir socio)
        public IActionResult OnGetResumen(int idCliente)
        {
            try
            {
                DataRow? r = _logica.ObtenerResumenSocio(idCliente);
                if (r == null || r["Membresia"] == DBNull.Value)
                    return new JsonResult(new { tienePlan = false });

                return new JsonResult(new
                {
                    tienePlan = true,
                    membresia = r["Membresia"].ToString(),
                    maximo = Convert.ToInt32(r["MaxClasesSemana"]),
                    usadas = Convert.ToInt32(r["Usadas"]),
                    vence = Convert.ToDateTime(r["FechaVencimiento"]).ToString("dd/MM/yyyy")
                });
            }
            catch (SqlException)
            {
                return StatusCode(503);
            }
        }

        public IActionResult OnPostGuardar()
        {
            List<string> errores = validacionOperacion.ValidarReserva(Reserva);

            // La reserva guarda al empleado que la captura
            int? idEmpleado = IdEmpleadoSesion();
            if (!idEmpleado.HasValue)
                errores.Add("Tu cuenta no está ligada a un empleado; no puedes capturar reservas.");

            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                // El SP bloquea la clase y valida cupo, membresía vigente y límite semanal
                MostrarRespuesta(_logica.Registrar(Reserva, idEmpleado!.Value, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al registrar la reserva.", "danger");
            }

            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

            return Redirigir();
        }

        // Cancelar reserva (usa la confirmación del Layout)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                MostrarRespuesta(_logica.Cancelar(id, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al cancelar la reserva.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(Busqueda, null, BusquedaFecha, BusquedaIdEstatus), "Reservas",
                ("Folio", "Folio"), ("Socio", "Socio"), ("Clase", "Clase"), ("Fecha", "Fecha"), ("Hora", "HoraInicio"),
                ("Salón", "Salon"), ("Sucursal", "Sucursal"), ("Solicitud", "FechaSolicitud"), ("Estatus", "Estatus"));
        }
    }
}
