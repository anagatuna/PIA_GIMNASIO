using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Gestión de usuarios (solo Administrador)
    public class UsuarioModel : PaginaProtegida
    {
        private readonly logicaUsuario _logica;
        private readonly logicaCatalogo _logicaCatalogo;

        public UsuarioModel(logicaUsuario logica, logicaCatalogo logicaCatalogo)
        {
            _logica = logica;
            _logicaCatalogo = logicaCatalogo;
            RolesPermitidos = new string[] { "ADMINISTRADOR" };
        }

        // Datos del formulario
        [BindProperty]
        public bdUsuario Usuario { get; set; } = new bdUsuario();

        // Filtros de búsqueda
        [BindProperty(SupportsGet = true)]
        public string? BusquedaCuenta { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdRol { get; set; }

        // Para los combos y la tabla
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Empleados { get; set; } = new List<SelectListItem>();
        public List<DataRow> Usuarios { get; set; } = new List<DataRow>();

        public void OnGet()
        {
            Cargar();
        }

        // Botón Editar: carga el usuario en el formulario
        public void OnGetEditar(int id)
        {
            try
            {
                Usuario = _logica.Obtener(id) ?? new bdUsuario();
            }
            catch (Exception)
            {
                Mensaje = "No se pudo cargar el usuario.";
                TipoMensaje = "danger";
            }
            Cargar();
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            // El estatus no se captura: siempre Activa (solo pasa a Inactiva con la baja lógica)
            Usuario.IdEstatus = 1;

            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                // El SP revisa que la cuenta no se repita y que el empleado esté libre
                bdRespuesta r = _logica.Guardar(Usuario, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                if (r.TipoMensaje == "success")
                    Limpiar();
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar el usuario.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Eliminar (baja lógica)
        public IActionResult OnPostEliminar(int id)
        {
            if (id == IdUsuarioSesion())
            {
                Mensaje = "No puedes eliminar tu propio usuario.";
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                bdRespuesta r = _logica.Eliminar(id, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al eliminar el usuario.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Exportar a Excel
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(BusquedaCuenta, BusquedaIdRol, null);
            return ExportarExcel(dt, "Usuarios",
                new string[] { "Cuenta", "Empleado", "Rol", "Fecha de registro" },
                new string[] { "Cuenta", "Empleado", "Rol", "FechaRegistro" });
        }

        // Llena combos y tabla
        private void Cargar()
        {
            try
            {
                Roles = CargarCombo(_logicaCatalogo.ConsultarRoles(), "IdCatalogo", "Descripcion");

                // Empleados activos sin cuenta (si se edita, incluye el empleado de ese usuario)
                Empleados = CargarCombo(_logica.ConsultarEmpleadosDisponibles(Usuario.IdUsuario), "IdEmpleado", "Empleado");

                Usuarios = Paginar(_logica.Consultar(BusquedaCuenta, BusquedaIdRol, null));
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // Reglas de captura. Regresa el primer error o "" si todo está bien.
        private string Validar()
        {
            bool esNuevo = Usuario.IdUsuario == 0;

            if (esNuevo && Usuario.IdEmpleado == null)
                return "Selecciona el empleado al que pertenece la cuenta.";
            if (string.IsNullOrWhiteSpace(Usuario.Cuenta))
                return "Ingresa la cuenta.";
            if (Usuario.Cuenta.Trim().Contains(' '))
                return "La cuenta no puede llevar espacios.";

            // Contraseña: obligatoria al registrar; al editar, vacía = conservar la actual
            if (esNuevo && string.IsNullOrEmpty(Usuario.Contrasenia))
                return "Ingresa la contraseña.";
            if (!string.IsNullOrEmpty(Usuario.Contrasenia) && (Usuario.Contrasenia.Length < 6 || Usuario.Contrasenia.Length > 50))
                return "La contraseña debe tener entre 6 y 50 caracteres.";

            if (Usuario.IdRol == null)
                return "Selecciona el rol.";

            // Evita que el administrador se quite el acceso a sí mismo
            if (!esNuevo && Usuario.IdUsuario == IdUsuarioSesion() && Usuario.IdRol != 1)
                return "No puedes cambiar tu propio rol.";

            return "";
        }

        private void Limpiar()
        {
            ModelState.Clear();
            Usuario = new bdUsuario();
        }
    }
}
