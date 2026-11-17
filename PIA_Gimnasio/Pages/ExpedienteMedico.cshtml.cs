using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Ficha clínica del socio. Administrador y Recepcionista editan; el Entrenador solo consulta.
    public class ExpedienteMedicoModel : PaginaProtegida
    {
        private readonly logicaExpediente _logica;

        public ExpedienteMedicoModel(logicaExpediente logica)
        {
            _logica = logica;
            RolesPermitidos = new string[] { "ADMINISTRADOR", "RECEPCIONISTA", "ENTRENADOR" };
        }

        [BindProperty]
        public bdExpediente Expediente { get; set; } = new bdExpediente();

        // Socio seleccionado en la tabla
        [BindProperty(SupportsGet = true)]
        public int? IdCliente { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public DataRow? Socio { get; set; }
        public List<SelectListItem> TiposSangre { get; set; } = new List<SelectListItem>();
        public List<DataRow> Socios { get; set; } = new List<DataRow>();

        public bool TieneExpediente
        {
            get { return Socio != null && Socio["IdExpediente"] != DBNull.Value; }
        }

        // Edad del socio a partir de su fecha de nacimiento
        public int Edad
        {
            get
            {
                if (Socio == null || Socio["FechaNacimiento"] == DBNull.Value)
                    return 0;

                DateTime nacimiento = Convert.ToDateTime(Socio["FechaNacimiento"]);
                int edad = DateTime.Today.Year - nacimiento.Year;

                // Si este año todavía no llega su cumpleaños, tiene un año menos
                if (DateTime.Today.Month < nacimiento.Month ||
                    (DateTime.Today.Month == nacimiento.Month && DateTime.Today.Day < nacimiento.Day))
                    edad--;

                return edad;
            }
        }

        // El entrenador solo puede leer
        public bool SoloLectura
        {
            get { return EsEntrenador; }
        }

        // La recepcionista solo ve los socios de su sucursal (null = todas)
        private int? SucursalPermitida()
        {
            if (RolSesion() == "RECEPCIONISTA")
                return IdSucursalSesion() ?? -1;
            return null;
        }

        // El entrenador solo ve a sus alumnos: socios con una rutina suya o con reserva en sus clases (null = todos)
        private int? EntrenadorPermitido()
        {
            if (EsEntrenador)
                return IdEmpleadoSesion() ?? -1;
            return null;
        }

        public void OnGet()
        {
            Cargar();

            // Llena el formulario con el expediente guardado
            if (Socio != null && TieneExpediente)
            {
                Expediente.IdCliente = Convert.ToInt32(IdCliente);
                Expediente.TipoSangre = Socio["TipoSangre"].ToString();
                Expediente.PadecimientosCronicos = Socio["PadecimientosCronicos"].ToString();
                Expediente.LesionesPrevias = Socio["LesionesPrevias"].ToString();
                Expediente.Alergias = Socio["Alergias"].ToString();
                Expediente.CondicionRiesgo = Socio["CondicionRiesgo"].ToString();
            }
        }

        // Botón Guardar expediente
        public IActionResult OnPostGuardar()
        {
            if (SoloLectura)
            {
                Mensaje = "Tu rol solo puede consultar el expediente.";
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            Expediente.IdCliente = IdCliente ?? 0;

            if (Expediente.IdCliente == 0)
                Mensaje = "Selecciona un socio.";
            else if (_logica.Obtener(Expediente.IdCliente, SucursalPermitida(), EntrenadorPermitido()) == null)
                Mensaje = "El socio no pertenece a tu sucursal.";
            else if (string.IsNullOrEmpty(Expediente.TipoSangre))
                Mensaje = "Selecciona el tipo de sangre.";

            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                bdRespuesta r = _logica.Guardar(Expediente, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar el expediente.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Llena la tabla y los datos del socio seleccionado
        private void Cargar()
        {
            TiposSangre = CargarCombo(logicaCatalogo.TiposSangre);
            try
            {
                Socios = Paginar(_logica.Consultar(Busqueda, SucursalPermitida(), EntrenadorPermitido()));

                // Si el socio no está entre los que puede ver, no se muestra su expediente
                if (IdCliente != null)
                    Socio = _logica.Obtener(IdCliente.Value, SucursalPermitida(), EntrenadorPermitido());
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
