using System.Data;
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
        private readonly logicaCatalogo _logicaCatalogo;

        public SocioModel(logicaSocio logica, logicaSucursal logicaSucursal, logicaCatalogo logicaCatalogo)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaCatalogo = logicaCatalogo;
            RolesPermitidos = new string[] { "ADMINISTRADOR", "RECEPCIONISTA" };
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

        // Filtro de membresía (-1 = con membresía, 0 = sin membresía, o el tipo)
        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdTipoMembresia { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposSangre { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Membresias { get; set; } = new List<SelectListItem>();
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

                // La recepcionista solo edita socios de su sucursal
                if (EsRecepcionista && Socio.IdCliente > 0 && Socio.IdSucursal != IdSucursalSesion())
                {
                    Socio = new bdSocio { IdSucursal = IdSucursalSesion() };
                    Mensaje = "El socio no pertenece a tu sucursal.";
                    TipoMensaje = "warning";
                }
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
            // La fecha fuera de rango se toma como vacía (la validación pide capturarla)
            Socio.FechaNacimiento = FechaValida(Socio.FechaNacimiento);

            // El estatus no se captura: siempre Activo (solo pasa a Inactivo con la baja lógica)
            bool esNuevo = Socio.IdCliente == 0;
            Socio.IdEstatus = 1;

            // La recepcionista solo registra socios en su sucursal asignada
            if (EsRecepcionista)
                Socio.IdSucursal = IdSucursalSesion();

            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                // El SP genera el folio y también guarda el expediente médico
                bdRespuesta r = _logica.Guardar(Socio, Expediente, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                // La membresía es obligatoria: el socio nuevo pasa a Suscripción ya seleccionado
                if (r.TipoMensaje == "success" && esNuevo && r.Id > 0)
                    return RedirectToPage("/Suscripcion", new { idCliente = r.Id, nuevo = true });

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
            LimitarSucursal();
            DataTable dt = _logica.Consultar(Busqueda, BusquedaIdSucursal, null, BusquedaIdTipoMembresia);
            return ExportarExcel(dt, "Socios",
                new string[] { "Folio", "Socio", "Correo", "Teléfono", "Sucursal", "Membresía", "Vencimiento", "Inscripción" },
                new string[] { "Folio", "Socio", "Correo", "Telefono", "Sucursal", "Membresia", "FechaVencimiento", "FechaInscripcion" });
        }

        // Llena combos y tabla
        private void Cargar()
        {
            TiposSangre = CargarCombo(logicaCatalogo.TiposSangre);
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");

                // La recepcionista solo ve su sucursal en el combo y en la tabla
                if (EsRecepcionista)
                {
                    List<SelectListItem> propia = new List<SelectListItem>();
                    foreach (SelectListItem s in Sucursales)
                    {
                        if (s.Value == IdSucursalSesion().ToString())
                            propia.Add(s);
                    }
                    Sucursales = propia;
                }
                LimitarSucursal();

                Membresias = CargarCombo(_logicaCatalogo.ConsultarMembresias(), "IdCatalogo", "Descripcion");
                Socios = Paginar(_logica.Consultar(Busqueda, BusquedaIdSucursal, null, BusquedaIdTipoMembresia));
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // La recepcionista solo consulta los socios de su sucursal
        private void LimitarSucursal()
        {
            if (EsRecepcionista)
                BusquedaIdSucursal = IdSucursalSesion() ?? -1;
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

            if (string.IsNullOrWhiteSpace(Socio.Correo) || !CorreoValido(Socio.Correo.Trim()))
                return "Ingresa un correo válido.";
            if (string.IsNullOrWhiteSpace(Socio.Telefono) || !SoloNumeros(Socio.Telefono.Trim(), 10))
                return "El teléfono debe tener 10 dígitos.";

            if (Socio.IdSucursal == null)
                return "Selecciona la sucursal de inscripción.";

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
