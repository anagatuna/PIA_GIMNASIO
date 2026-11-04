using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Catálogo de puestos. Permisos: Seguridad/Modulos.cs (solo Administrador)
    public class PuestoModel : PaginaBase
    {
        private readonly logicaPuesto _logica;

        public PuestoModel(logicaPuesto logica)
        {
            _logica = logica;
        }

        [BindProperty]
        public bdPuesto Puesto { get; set; } = new bdPuesto();

        [BindProperty(SupportsGet = true)]
        public string? BusquedaNombre { get; set; }

        public List<DataRow> Puestos { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaNombre", BusquedaNombre));
        }

        private void CargarPantalla()
        {
            try
            {
                Puestos = Paginar(_logica.Consultar(BusquedaNombre));
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

            bdPuesto? puesto = _logica.Obtener(id);
            if (puesto != null)
                Puesto = puesto;
            else
                MostrarMensaje("No se encontró el puesto seleccionado.", "warning");
        }

        public IActionResult OnPostGuardar()
        {
            List<string> errores = validacionCatalogo.Validar(Puesto.Nombre, Puesto.Descripcion);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Puesto, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el puesto.", "danger");
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
                MostrarMensaje("Ocurrió un error al eliminar el puesto.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(BusquedaNombre), "Puestos",
                ("Puesto", "Nombre"), ("Descripción", "Descripcion"), ("Empleados", "TotalEmpleados"));
        }
    }
}
