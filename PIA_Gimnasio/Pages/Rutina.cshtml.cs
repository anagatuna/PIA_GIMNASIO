using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Permisos: Seguridad/Modulos.cs
    public class RutinaModel : PaginaBase
    {
        private readonly logicaRutina _logica;

        public RutinaModel(logicaRutina logica)
        {
            _logica = logica;
        }

        // Filtros de búsqueda (vienen en la URL para conservarse al paginar)
        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdCliente { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEmpleado { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BusquedaObjetivo { get; set; }

        // Para los combos y la tabla
        public List<SelectListItem> Socios { get; set; } = new();
        public List<SelectListItem> Entrenadores { get; set; } = new();
        public List<SelectListItem> Objetivos { get; } = CargarCombo(validacionRutina.Objetivos);
        public List<DataRow> Rutinas { get; set; } = new();

        // El entrenador no filtra por entrenador: siempre ve las suyas
        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaIdCliente", BusquedaIdCliente),
                ("BusquedaIdEmpleado", EsEntrenador ? null : BusquedaIdEmpleado), ("BusquedaObjetivo", BusquedaObjetivo));
        }

        // Al abrir la página o al buscar
        public void OnGet()
        {
            // El entrenador solo ve las rutinas que él diseñó (sin empleado ligado no ve ninguna)
            if (EsEntrenador)
                BusquedaIdEmpleado = IdEmpleadoSesion() ?? -1;

            try
            {
                Socios = CargarCombo(_logica.ConsultarSocios(), "IdCliente", "Socio");
                Entrenadores = CargarCombo(_logica.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");
                Rutinas = Paginar(_logica.Consultar(BusquedaIdCliente, BusquedaIdEmpleado, BusquedaObjetivo));
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        // Botón Eliminar (baja lógica)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                MostrarRespuesta(_logica.Eliminar(id, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al eliminar la rutina.", "danger");
            }

            return Redirigir();
        }
    }
}
