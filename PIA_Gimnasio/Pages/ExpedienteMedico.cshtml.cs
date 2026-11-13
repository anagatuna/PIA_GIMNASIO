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
            RolesPermitidos = new string[] { "Administrador", "Recepcionista", "Entrenador" };
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

        // El entrenador solo puede leer
        public bool SoloLectura
        {
            get { return EsEntrenador; }
        }

        public void OnGet()
        {
            Cargar();

            // Llena el formulario con el expediente guardado
            if (TieneExpediente)
            {
                Expediente.IdCliente = IdCliente!.Value;
                Expediente.TipoSangre = Socio!["TipoSangre"].ToString();
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
            TiposSangre = CargarCombo(logicaSocio.TiposSangre);
            try
            {
                Socios = Paginar(_logica.Consultar(Busqueda));

                if (IdCliente != null)
                    Socio = _logica.Obtener(IdCliente.Value);
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
