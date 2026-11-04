using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Gestión de empleados con especialidades dinámicas. Permisos: Seguridad/Modulos.cs (solo Administrador)
    public class EmpleadoModel : PaginaBase
    {
        private readonly logicaEmpleado _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaEjercicio _logicaEstatus;   // Reutiliza el combo Activo / Inactivo

        public EmpleadoModel(logicaEmpleado logica, logicaSucursal logicaSucursal, logicaEjercicio logicaEstatus)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaEstatus = logicaEstatus;
        }

        [BindProperty]
        public bdEmpleado Empleado { get; set; } = new bdEmpleado();

        [BindProperty(SupportsGet = true)]
        public string? BusquedaNombre { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdSucursal { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdPuesto { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public List<SelectListItem> Puestos { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<SelectListItem> Especialidades { get; set; } = new();
        public List<DataRow> Empleados { get; set; } = new();

        // Id del puesto "Entrenador": el JavaScript habilita las especialidades solo con ese puesto
        public int IdPuestoEntrenador { get; set; }
        public bool EsPuestoEntrenador => Empleado.IdPuesto.HasValue && Empleado.IdPuesto == IdPuestoEntrenador;

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaNombre", BusquedaNombre), ("BusquedaIdSucursal", BusquedaIdSucursal),
                ("BusquedaIdPuesto", BusquedaIdPuesto), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        private void CargarPantalla()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Puestos = CargarCombo(_logica.ConsultarPuestos(), "IdPuesto", "Nombre");
                Estatus = CargarCombo(_logicaEstatus.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Especialidades = CargarCombo(_logica.ConsultarEspecialidades(), "IdEspecialidad", "Nombre");
                IdPuestoEntrenador = Puestos.Where(p => p.Text.Equals("Entrenador", StringComparison.OrdinalIgnoreCase))
                    .Select(p => int.Parse(p.Value)).FirstOrDefault();

                Empleados = Paginar(_logica.Consultar(BusquedaNombre, BusquedaIdSucursal, BusquedaIdPuesto, BusquedaIdEstatus));
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

            bdEmpleado? empleado = _logica.Obtener(id);
            if (empleado != null)
                Empleado = empleado;
            else
                MostrarMensaje("No se encontró el empleado seleccionado.", "warning");
        }

        public IActionResult OnPostGuardar()
        {
            CargarPantalla();

            // Las especialidades solo aplican al puesto Entrenador
            if (!EsPuestoEntrenador)
                Empleado.Especialidades.Clear();

            List<string> errores = validacionEmpleado.Validar(Empleado, EsPuestoEntrenador);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Empleado, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el empleado.", "danger");
            }

            if (TipoMensaje != "success")
                return Page();

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
                MostrarMensaje("Ocurrió un error al eliminar el empleado.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(BusquedaNombre, BusquedaIdSucursal, BusquedaIdPuesto, BusquedaIdEstatus), "Empleados",
                ("Empleado", "Empleado"), ("Puesto", "Puesto"), ("Especialidades", "Especialidades"), ("Sucursal", "Sucursal"),
                ("Correo", "Correo"), ("Teléfono", "Telefono"), ("CURP", "CURP"), ("RFC", "RFC"),
                ("Fecha de contratación", "FechaContratacion"), ("Cuenta de usuario", "Cuenta"), ("Estatus", "Estatus"));
        }
    }
}
