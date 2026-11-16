using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Venta y renovación de membresías: vigencia de 1 mes y máximo 1 suscripción activa por socio.
    // Administrador y Recepcionista.
    public class SuscripcionModel : PaginaProtegida
    {
        private readonly logicaSuscripcion _logica;
        private readonly logicaSocio _logicaSocio;
        private readonly logicaMembresia _logicaMembresia;

        public SuscripcionModel(logicaSuscripcion logica, logicaSocio logicaSocio, logicaMembresia logicaMembresia)
        {
            _logica = logica;
            _logicaSocio = logicaSocio;
            _logicaMembresia = logicaMembresia;
            RolesPermitidos = new string[] { "Administrador", "Recepcionista" };
        }

        [BindProperty]
        public bdSuscripcion Suscripcion { get; set; } = new bdSuscripcion();

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdTipoMembresia { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Socios { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Membresias { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> MembresiasFiltro { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Estatus { get; set; } = new List<SelectListItem>();
        public List<DataRow> Suscripciones { get; set; } = new List<DataRow>();

        // true cuando viene de registrar un socio nuevo: al vender su membresía regresa a Socios
        [BindProperty(SupportsGet = true)]
        public bool Nuevo { get; set; }

        // idCliente llega desde "Vender" o desde el registro de un socio nuevo (Socios)
        public void OnGet(int? idCliente)
        {
            Suscripcion.IdCliente = idCliente;
            if (Nuevo)
            {
                Mensaje = "Socio registrado correctamente. La membresía es obligatoria: registra ahora su suscripción.";
                TipoMensaje = "success";
            }
            Cargar();
        }

        // Botón Renovar (tabla)
        public IActionResult OnPostRenovar(int id)
        {
            try
            {
                bdRespuesta r = _logica.Renovar(id, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al renovar la suscripción.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Registrar
        public IActionResult OnPostGuardar()
        {
            if (Suscripcion.IdCliente == null)
                Mensaje = "Selecciona el socio.";
            else if (Suscripcion.IdTipoMembresia == null)
                Mensaje = "Selecciona la membresía.";
            else if (Suscripcion.FechaInicio == null)
                Mensaje = "Ingresa la fecha de inicio.";
            else if (Suscripcion.FechaInicio < DateTime.Today)
                Mensaje = "La fecha de inicio no puede ser anterior a hoy.";
            else if (Suscripcion.FechaInicio > DateTime.Today.AddMonths(1))
                Mensaje = "La fecha de inicio no puede ser mayor a un mes a partir de hoy.";

            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                // El SP aplica la regla de 1 suscripción activa y calcula la vigencia de 1 mes
                bdRespuesta r = _logica.Registrar(Suscripcion, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                if (r.TipoMensaje == "success")
                {
                    ModelState.Clear();
                    Suscripcion = new bdSuscripcion();

                    // Socio recién registrado: al aceptar el aviso vuelve a la pestaña de Socios
                    if (Nuevo)
                        RegresarA = "/Socio";
                }
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al registrar la suscripción.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Cancelar suscripción (usa la ventana de confirmación)
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
                Mensaje = "Ocurrió un error al cancelar la suscripción.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Exportar a Excel
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(Busqueda, BusquedaIdTipoMembresia, BusquedaIdEstatus);
            return ExportarExcel(dt, "Suscripciones",
                new string[] { "Folio", "Socio", "Membresía", "Precio", "Inicio", "Vencimiento", "Estatus" },
                new string[] { "Folio", "Socio", "Membresia", "Precio", "FechaInicio", "FechaVencimiento", "Estatus" });
        }

        // Llena combos y tabla
        private void Cargar()
        {
            try
            {
                Socios = CargarCombo(_logicaSocio.ConsultarActivos(), "IdCliente", "Socio");
                Membresias = CargarCombo(_logicaMembresia.ConsultarActivas(), "IdTipoMembresia", "Membresia");
                MembresiasFiltro = CargarCombo(_logicaMembresia.Consultar(), "IdTipoMembresia", "Nombre");
                Estatus = CargarCombo(_logica.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Suscripciones = Paginar(_logica.Consultar(Busqueda, BusquedaIdTipoMembresia, BusquedaIdEstatus));
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
