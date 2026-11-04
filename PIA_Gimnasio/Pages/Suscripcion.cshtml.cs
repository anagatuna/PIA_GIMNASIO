using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Venta y renovación de membresías: vigencia de 1 mes y máximo 1 suscripción activa por socio.
    // Permisos: Seguridad/Modulos.cs (Administrador y Recepcionista)
    public class SuscripcionModel : PaginaBase
    {
        private readonly logicaSuscripcion _logica;
        private readonly logicaSocio _logicaSocio;
        private readonly logicaMembresia _logicaMembresia;

        public SuscripcionModel(logicaSuscripcion logica, logicaSocio logicaSocio, logicaMembresia logicaMembresia)
        {
            _logica = logica;
            _logicaSocio = logicaSocio;
            _logicaMembresia = logicaMembresia;
        }

        [BindProperty]
        public bdSuscripcion Suscripcion { get; set; } = new bdSuscripcion();

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdTipoMembresia { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Socios { get; set; } = new();
        public List<SelectListItem> Membresias { get; set; } = new();
        public List<SelectListItem> MembresiasFiltro { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<DataRow> Suscripciones { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Busqueda", Busqueda), ("BusquedaIdTipoMembresia", BusquedaIdTipoMembresia), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        private void CargarPantalla()
        {
            try
            {
                Socios = CargarCombo(_logicaSocio.ConsultarActivos(), "IdCliente", "Socio");
                DataTable membresias = _logicaMembresia.ConsultarActivas();
                Membresias = CargarCombo(membresias, "IdTipoMembresia", "Membresia");
                MembresiasFiltro = CargarCombo(_logicaMembresia.Consultar(null, null), "IdTipoMembresia", "Nombre");
                Estatus = CargarCombo(_logica.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Suscripciones = Paginar(_logica.Consultar(Busqueda, BusquedaIdTipoMembresia, BusquedaIdEstatus));
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        // idCliente / idTipoMembresia llegan desde "Vender" (Socios) o "Renovar" (tabla)
        public void OnGet(int? idCliente, int? idTipoMembresia)
        {
            Suscripcion.IdCliente = idCliente;
            Suscripcion.IdTipoMembresia = idTipoMembresia;
            CargarPantalla();
        }

        public IActionResult OnPostGuardar()
        {
            List<string> errores = validacionOperacion.ValidarSuscripcion(Suscripcion);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                // El SP aplica la regla de 1 suscripción activa y calcula la vigencia de 1 mes
                MostrarRespuesta(_logica.Registrar(Suscripcion, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al registrar la suscripción.", "danger");
            }

            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

            return Redirigir();
        }

        // Botón Cancelar (usa la confirmación del Layout)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                MostrarRespuesta(_logica.Cancelar(id, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al cancelar la suscripción.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(Busqueda, BusquedaIdTipoMembresia, BusquedaIdEstatus), "Suscripciones",
                ("Folio", "Folio"), ("Socio", "Socio"), ("Membresía", "Membresia"), ("Precio", "Precio"),
                ("Inicio", "FechaInicio"), ("Vencimiento", "FechaVencimiento"), ("Estatus", "Estatus"));
        }
    }
}
