using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Tipos de membresía (Bronce, Plata, Oro...). Permisos: Seguridad/Modulos.cs (solo Administrador)
    public class MembresiaModel : PaginaBase
    {
        private readonly logicaMembresia _logica;
        private readonly logicaEjercicio _logicaEstatus;   // Reutiliza el combo Activo / Inactivo

        public MembresiaModel(logicaMembresia logica, logicaEjercicio logicaEstatus)
        {
            _logica = logica;
            _logicaEstatus = logicaEstatus;
        }

        [BindProperty]
        public bdMembresia Membresia { get; set; } = new bdMembresia();

        [BindProperty(SupportsGet = true)]
        public string? BusquedaNombre { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Estatus { get; set; } = new();
        public List<SelectListItem> Restricciones { get; } = CargarCombo(validacionCatalogo.RestriccionesAforo);
        public List<DataRow> Membresias { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaNombre", BusquedaNombre), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        private void CargarPantalla()
        {
            try
            {
                Estatus = CargarCombo(_logicaEstatus.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Membresias = Paginar(_logica.Consultar(BusquedaNombre, BusquedaIdEstatus));
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

        public void OnGetEditar(int id)
        {
            CargarPantalla();

            bdMembresia? membresia = _logica.Obtener(id);
            if (membresia != null)
                Membresia = membresia;
            else
                MostrarMensaje("No se encontró la membresía seleccionada.", "warning");
        }

        public IActionResult OnPostGuardar()
        {
            List<string> errores = validacionCatalogo.ValidarMembresia(Membresia);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Membresia, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar la membresía.", "danger");
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
            try
            {
                MostrarRespuesta(_logica.Eliminar(id, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al eliminar la membresía.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(BusquedaNombre, BusquedaIdEstatus), "Membresias",
                ("Membresía", "Nombre"), ("Precio mensual", "Precio"), ("Clases por semana", "MaxClasesSemana"),
                ("Restricción de aforo", "RestriccionAforo"), ("Descripción", "Descripcion"),
                ("Suscripciones activas", "SuscripcionesActivas"), ("Estatus", "Estatus"));
        }
    }
}
