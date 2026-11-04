using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;
using PIA_Gimnasio.Seguridad;

namespace PIA_Gimnasio.Pages
{
    // Registro de socios con folio automático y expediente médico obligatorio.
    // Permisos: Seguridad/Modulos.cs (Administrador y Recepcionista; solo el Administrador elimina)
    public class SocioModel : PaginaBase
    {
        private readonly logicaSocio _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEjercicio _logicaEstatus;   // Reutiliza el combo Activo / Inactivo

        public SocioModel(logicaSocio logica, logicaSucursal logicaSucursal, logicaEjercicio logicaEstatus)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEstatus = logicaEstatus;
        }

        [BindProperty]
        public bdSocio Socio { get; set; } = new bdSocio();

        // Expediente médico: se captura junto con el socio al registrarlo
        [BindProperty]
        public bdExpediente Expediente { get; set; } = new bdExpediente();

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdSucursal { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<SelectListItem> TiposSangre { get; } = CargarCombo(validacionSocio.TiposSangre);
        public List<DataRow> Socios { get; set; } = new();

        public bool PuedeEliminar => User.IsInRole(Roles.Administrador);

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Busqueda", Busqueda), ("BusquedaIdSucursal", BusquedaIdSucursal), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        private void CargarPantalla()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Estatus = CargarCombo(_logicaEstatus.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Socios = Paginar(_logica.Consultar(Busqueda, BusquedaIdSucursal, BusquedaIdEstatus));
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public void OnGet()
        {
            // La recepcionista registra en su propia sucursal por omisión
            if (int.TryParse(User.FindFirst("IdSucursal")?.Value, out int idSucursal))
                Socio.IdSucursal = idSucursal;

            CargarPantalla();
        }

        public void OnGetEditar(int id)
        {
            CargarPantalla();

            bdSocio? socio = _logica.Obtener(id);
            if (socio != null)
                Socio = socio;
            else
                MostrarMensaje("No se encontró el socio seleccionado.", "warning");
        }

        public IActionResult OnPostGuardar()
        {
            bool esNuevo = Socio.IdCliente == 0;

            List<string> errores = validacionSocio.Validar(Socio);
            if (esNuevo)
                errores.AddRange(validacionSocio.ValidarExpediente(Expediente));   // Expediente obligatorio al registrar

            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                var (respuesta, _) = _logica.Guardar(Socio, Expediente, IdUsuarioSesion());
                MostrarRespuesta(respuesta);
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el socio.", "danger");
            }

            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

            return Redirigir();
        }

        public IActionResult OnPostEliminar(int id)
        {
            if (!PuedeEliminar)
                return Forbid();

            try
            {
                MostrarRespuesta(_logica.Eliminar(id, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al eliminar el socio.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(Busqueda, BusquedaIdSucursal, BusquedaIdEstatus), "Socios",
                ("Folio", "Folio"), ("Socio", "Socio"), ("Correo", "Correo"), ("Teléfono", "Telefono"),
                ("Sucursal", "Sucursal"), ("Membresía", "Membresia"), ("Vencimiento", "FechaVencimiento"),
                ("Inscripción", "FechaInscripcion"), ("Estatus", "Estatus"));
        }
    }
}
