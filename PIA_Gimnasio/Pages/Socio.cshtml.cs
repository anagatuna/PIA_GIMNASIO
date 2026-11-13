using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Registro de socios con folio automático y expediente médico obligatorio.
    // Administrador y Recepcionista; solo el Administrador elimina.
    public class SocioModel : PaginaProtegida
    {
        private readonly logicaSocio _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEjercicio _logicaEstatus;   // Reutiliza el combo Activo / Inactivo

        public SocioModel(logicaSocio logica, logicaSucursal logicaSucursal, logicaEjercicio logicaEstatus)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEstatus = logicaEstatus;
            RolesPermitidos = new string[] { "Administrador", "Recepcionista" };
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

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Estatus { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposSangre { get; set; } = new List<SelectListItem>();
        public List<DataRow> Socios { get; set; } = new List<DataRow>();

        public bool PuedeEliminar
        {
            get { return EsAdministrador; }
        }

        public void OnGet()
        {
            // La recepcionista registra en su propia sucursal por omisión
            Socio.IdSucursal = IdSucursalSesion();
            Cargar();
        }

        // Botón Editar: carga el socio en el formulario
        public void OnGetEditar(int id)
        {
            try
            {
                Socio = _logica.Obtener(id) ?? new bdSocio();
            }
            catch (Exception)
            {
                Mensaje = "No se pudo cargar el socio.";
                TipoMensaje = "danger";
            }
            Cargar();
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                // El SP genera el folio y guarda el expediente en la misma transacción
                bdRespuesta r = _logica.Guardar(Socio, Expediente, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                if (r.TipoMensaje == "success")
                    Limpiar();
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar el socio.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Eliminar (baja lógica, solo Administrador)
        public IActionResult OnPostEliminar(int id)
        {
            if (!PuedeEliminar)
            {
                Mensaje = "Solo el administrador puede eliminar socios.";
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                bdRespuesta r = _logica.Eliminar(id, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al eliminar el socio.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Exportar a Excel
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(Busqueda, BusquedaIdSucursal, BusquedaIdEstatus);
            return ExportarExcel(dt, "Socios",
                new string[] { "Folio", "Socio", "Correo", "Teléfono", "Sucursal", "Membresía", "Vencimiento", "Inscripción", "Estatus" },
                new string[] { "Folio", "Socio", "Correo", "Telefono", "Sucursal", "Membresia", "FechaVencimiento", "FechaInscripcion", "Estatus" });
        }

        // Llena combos y tabla
        private void Cargar()
        {
            TiposSangre = CargarCombo(logicaSocio.TiposSangre);
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Estatus = CargarCombo(_logicaEstatus.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Socios = Paginar(_logica.Consultar(Busqueda, BusquedaIdSucursal, BusquedaIdEstatus));
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // Reglas de captura. Regresa el primer error o "" si todo está bien.
        private string Validar()
        {
            if (string.IsNullOrWhiteSpace(Socio.Nombre) || string.IsNullOrWhiteSpace(Socio.PrimerApellido))
                return "Ingresa el nombre y el primer apellido.";

            if (Socio.FechaNacimiento == null)
                return "Ingresa la fecha de nacimiento.";
            if (Socio.FechaNacimiento > DateTime.Today.AddYears(-12) || Socio.FechaNacimiento < DateTime.Today.AddYears(-100))
                return "El socio debe tener entre 12 y 100 años.";

            if (string.IsNullOrWhiteSpace(Socio.Correo) || !Regex.IsMatch(Socio.Correo.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Ingresa un correo válido.";
            if (string.IsNullOrWhiteSpace(Socio.Telefono) || !Regex.IsMatch(Socio.Telefono.Trim(), @"^\d{10}$"))
                return "El teléfono debe tener 10 dígitos.";

            if (Socio.IdSucursal == null)
                return "Selecciona la sucursal de inscripción.";
            if (Socio.IdEstatus == null)
                return "Selecciona el estatus.";

            // Expediente médico obligatorio al registrar
            if (Socio.IdCliente == 0 && string.IsNullOrEmpty(Expediente.TipoSangre))
                return "Selecciona el tipo de sangre (el expediente médico es obligatorio).";

            return "";
        }

        private void Limpiar()
        {
            ModelState.Clear();
            Socio = new bdSocio();
            Expediente = new bdExpediente();
        }
    }
}
