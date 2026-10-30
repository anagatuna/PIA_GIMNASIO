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
    public class UsuarioModel : PaginaBase
    {
        private readonly logicaUsuario _logica;

        public UsuarioModel(logicaUsuario logica)
        {
            _logica = logica;
        }

        // Datos del formulario
        [BindProperty]
        public bdUsuario Usuario { get; set; } = new bdUsuario();

        // Filtros de búsqueda (vienen en la URL para conservarse al paginar)
        [BindProperty(SupportsGet = true)]
        public string? BusquedaCuenta { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdRol { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        // Para los combos y la tabla
        public List<SelectListItem> Roles { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<SelectListItem> Empleados { get; set; } = new();
        public List<DataRow> Usuarios { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaCuenta", BusquedaCuenta), ("BusquedaIdRol", BusquedaIdRol), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        // Llena combos y tabla (con paginación)
        private void CargarPantalla()
        {
            try
            {
                Roles = CargarCombo(_logica.ConsultarRoles(), "IdRol", "Descripcion");
                Estatus = CargarCombo(_logica.ConsultarEstatus(), "IdEstatus", "Descripcion");

                // Empleados activos sin cuenta (si se edita, incluye el empleado de ese usuario)
                Empleados = CargarCombo(_logica.ConsultarEmpleadosDisponibles(Usuario.IdUsuario), "IdEmpleado", "Empleado");

                Usuarios = Paginar(_logica.Consultar(BusquedaCuenta, BusquedaIdRol, BusquedaIdEstatus));
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

        // Botón Editar: carga el usuario en el formulario
        public void OnGetEditar(int id)
        {
            bdUsuario? usuario = _logica.Obtener(id);
            if (usuario != null)
                Usuario = usuario;
            else
                MostrarMensaje("No se encontró el usuario seleccionado.", "warning");

            // Después de leer el usuario, para que el combo incluya a su empleado
            CargarPantalla();
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            // Reglas de captura (Logica/Validacion/validacionUsuario.cs)
            List<string> errores = validacionUsuario.Validar(Usuario, IdUsuarioSesion());

            if (errores.Count == 0)
            {
                try
                {
                    // Cuenta única: se revisa antes de mandar a registrar (la tabla ya no tiene UNIQUE)
                    if (_logica.ExisteCuenta(Usuario.Cuenta!, Usuario.IdUsuario))
                        errores.Add($"La cuenta \"{Usuario.Cuenta!.Trim()}\" ya está registrada.");

                    // Relación 1:1: al registrar, el empleado debe estar activo y sin cuenta
                    if (Usuario.IdUsuario == 0 && !_logica.ConsultarEmpleadosDisponibles(0).AsEnumerable()
                            .Any(r => Convert.ToInt32(r["IdEmpleado"]) == Usuario.IdEmpleado))
                        errores.Add("El empleado seleccionado no está activo o ya tiene una cuenta asignada.");
                }
                catch (SqlException)
                {
                    MostrarErrorConexion();
                    CargarPantalla();
                    return Page();
                }
            }

            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Usuario, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el usuario.", "danger");
            }

            // Si no se guardó (por ejemplo, cuenta repetida), se conservan los datos para corregirlos
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
            if (id == IdUsuarioSesion())
            {
                MostrarMensaje("No puedes eliminar tu propio usuario.", "warning");
                return Redirigir();
            }

            try
            {
                MostrarRespuesta(_logica.Eliminar(id, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al eliminar el usuario.", "danger");
            }

            return Redirigir();
        }

        // Exportar a Excel
        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(BusquedaCuenta, BusquedaIdRol, BusquedaIdEstatus), "Usuarios",
                ("Cuenta", "Cuenta"), ("Empleado", "Empleado"), ("Rol", "Rol"), ("Estatus", "Estatus"), ("Fecha de registro", "FechaRegistro"));
        }
    }
}
