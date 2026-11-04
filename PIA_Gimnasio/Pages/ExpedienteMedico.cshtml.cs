using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Ficha clínica del socio. Permisos: Seguridad/Modulos.cs (Administrador y Recepcionista editan; Entrenador solo lectura)
    public class ExpedienteMedicoModel : PaginaBase
    {
        private readonly logicaExpediente _logica;

        public ExpedienteMedicoModel(logicaExpediente logica)
        {
            _logica = logica;
        }

        [BindProperty]
        public bdExpediente Expediente { get; set; } = new bdExpediente();

        [BindProperty(SupportsGet = true)]
        public int? IdCliente { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        public DataRow? Socio { get; set; }
        public bool TieneExpediente => Socio != null && Socio["IdExpediente"] != DBNull.Value;
        public bool SoloLectura => EsEntrenador;
        public List<SelectListItem> TiposSangre { get; } = CargarCombo(validacionSocio.TiposSangre);
        public List<DataRow> Socios { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("Busqueda", Busqueda), ("IdCliente", IdCliente));
        }

        private void CargarPantalla()
        {
            try
            {
                Socios = Paginar(_logica.Consultar(Busqueda));

                if (IdCliente.HasValue)
                {
                    Socio = _logica.Obtener(IdCliente.Value);
                    if (Socio == null)
                        MostrarMensaje("No se encontró el socio seleccionado.", "warning");
                }
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        public void OnGet()
        {
            CargarPantalla();

            // Llena el formulario con el expediente guardado
            if (TieneExpediente)
            {
                Expediente = new bdExpediente
                {
                    IdCliente = IdCliente!.Value,
                    TipoSangre = Socio!["TipoSangre"].ToString(),
                    PadecimientosCronicos = Socio["PadecimientosCronicos"] as string,
                    LesionesPrevias = Socio["LesionesPrevias"] as string,
                    Alergias = Socio["Alergias"] as string,
                    CondicionRiesgo = Socio["CondicionRiesgo"] as string
                };
            }
        }

        public IActionResult OnPostGuardar()
        {
            if (SoloLectura)
                return Forbid();

            Expediente.IdCliente = IdCliente ?? 0;

            List<string> errores = validacionSocio.ValidarExpediente(Expediente);
            if (Expediente.IdCliente == 0)
                errores.Insert(0, "Selecciona un socio.");

            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Expediente, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el expediente.", "danger");
            }

            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

            return Redirigir();
        }
    }
}
