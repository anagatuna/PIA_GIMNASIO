using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Catálogo de especialidades. Permisos: Seguridad/Modulos.cs (solo Administrador)
    public class EspecialidadModel : PaginaBase
    {
        private readonly logicaEspecialidad _logica;

        public EspecialidadModel(logicaEspecialidad logica)
        {
            _logica = logica;
        }

        [BindProperty]
        public bdEspecialidad Especialidad { get; set; } = new bdEspecialidad();

        [BindProperty(SupportsGet = true)]
        public string? BusquedaNombre { get; set; }

        public List<DataRow> Especialidades { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaNombre", BusquedaNombre));
        }

        private void CargarPantalla()
        {
            try
            {
                Especialidades = Paginar(_logica.Consultar(BusquedaNombre));
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

            bdEspecialidad? especialidad = _logica.Obtener(id);
            if (especialidad != null)
                Especialidad = especialidad;
            else
                MostrarMensaje("No se encontró la especialidad seleccionada.", "warning");
        }

        public IActionResult OnPostGuardar()
        {
            List<string> errores = validacionCatalogo.Validar(Especialidad.Nombre, Especialidad.Descripcion);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Especialidad, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar la especialidad.", "danger");
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
                MostrarMensaje("Ocurrió un error al eliminar la especialidad.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(BusquedaNombre), "Especialidades",
                ("Especialidad", "Nombre"), ("Descripción", "Descripcion"), ("Entrenadores", "TotalEntrenadores"));
        }
    }
}
