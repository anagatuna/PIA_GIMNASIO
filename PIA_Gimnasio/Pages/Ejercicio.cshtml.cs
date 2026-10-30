using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Permisos: Seguridad/Modulos.cs
    public class EjercicioModel : PaginaBase
    {
        private readonly logicaEjercicio _logica;

        public EjercicioModel(logicaEjercicio logica)
        {
            _logica = logica;
        }

        // Datos del formulario
        [BindProperty]
        public bdEjercicio Ejercicio { get; set; } = new bdEjercicio();

        // Filtros de búsqueda (vienen en la URL para conservarse al paginar)
        [BindProperty(SupportsGet = true)]
        public string? BusquedaNombre { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BusquedaGrupo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BusquedaNivel { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        // Para los combos y la tabla
        public List<SelectListItem> Grupos { get; } = CargarCombo(validacionEjercicio.GruposMusculares);
        public List<SelectListItem> Niveles { get; } = CargarCombo(validacionEjercicio.NivelesDificultad);
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<DataRow> Ejercicios { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaNombre", BusquedaNombre), ("BusquedaGrupo", BusquedaGrupo),
                ("BusquedaNivel", BusquedaNivel), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        // Llena combo de estatus y tabla (con paginación)
        private void CargarPantalla()
        {
            try
            {
                Estatus = CargarCombo(_logica.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Ejercicios = Paginar(_logica.Consultar(BusquedaNombre, BusquedaGrupo, BusquedaNivel, BusquedaIdEstatus));
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        // Al abrir la página o al buscar
        public void OnGet()
        {
            CargarPantalla();
        }

        // Botón Editar: carga el ejercicio en el formulario
        public void OnGetEditar(int id)
        {
            CargarPantalla();

            bdEjercicio? ejercicio = _logica.Obtener(id);
            if (ejercicio != null)
                Ejercicio = ejercicio;
            else
                MostrarMensaje("No se encontró el ejercicio seleccionado.", "warning");
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            // Reglas de captura, incluido el nivel de dificultad (Logica/Validacion/validacionEjercicio.cs)
            List<string> errores = validacionEjercicio.Validar(Ejercicio);

            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Ejercicio, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el ejercicio.", "danger");
            }

            // Si no se guardó (por ejemplo, nombre repetido), se conservan los datos para corregirlos
            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

            return Redirigir();
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
                MostrarMensaje("Ocurrió un error al eliminar el ejercicio.", "danger");
            }

            return Redirigir();
        }

        // Exportar a Excel
        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(BusquedaNombre, BusquedaGrupo, BusquedaNivel, BusquedaIdEstatus), "Ejercicios",
                ("Ejercicio", "Nombre"), ("Grupo muscular", "GrupoMuscular"), ("Nivel de dificultad", "NivelDificultad"),
                ("Contraindicación", "ContraIndicacion"), ("Estatus", "Estatus"));
        }
    }
}
