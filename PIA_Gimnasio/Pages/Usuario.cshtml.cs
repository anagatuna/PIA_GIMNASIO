using System.Data;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Solo el Administrador puede gestionar usuarios
    [Authorize(Roles = "Administrador")]
    public class UsuarioModel : PageModel
    {
        private const int TamanioPagina = 10;
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

        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 1;

        // Para los combos, la tabla y el aviso
        public List<SelectListItem> Roles { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<DataRow> Usuarios { get; set; } = new();
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
        public string Mensaje { get; set; } = "";
        public string TipoMensaje { get; set; } = "";

        #region Utilidades

        // Muestra el mensaje del SP
        private void MostrarRespuesta(bdRespuesta r)
        {
            Mensaje = r.Mensaje;
            TipoMensaje = r.TipoMensaje;
        }

        private void MostrarMensaje(string mensaje, string tipo)
        {
            MostrarRespuesta(new bdRespuesta { Mensaje = mensaje, TipoMensaje = tipo });
        }

        // Guarda el mensaje en TempData para mostrarlo después del redirect y regresa a la búsqueda actual
        private IActionResult Redirigir()
        {
            TempData["Mensaje"] = Mensaje;
            TempData["TipoMensaje"] = TipoMensaje;
            return RedirectToPage(RutaFiltros());
        }

        // Id del usuario que inició sesión, para la auditoría
        private int IdUsuarioSesion()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : 0;
        }

        // Filtros actuales, para regresar a la misma búsqueda después de guardar o eliminar
        public Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            var ruta = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(BusquedaCuenta)) ruta["BusquedaCuenta"] = BusquedaCuenta.Trim();
            if (BusquedaIdRol.HasValue) ruta["BusquedaIdRol"] = BusquedaIdRol.Value.ToString();
            if (BusquedaIdEstatus.HasValue) ruta["BusquedaIdEstatus"] = BusquedaIdEstatus.Value.ToString();
            ruta["Pagina"] = (pagina ?? Pagina).ToString();
            return ruta;
        }

        #endregion

        #region Carga inicial

        // Llena combos y tabla (con paginación)
        private void CargarPantalla()
        {
            try
            {
                Roles = _logica.ConsultarRoles().AsEnumerable()
                    .Select(r => new SelectListItem(r["Descripcion"].ToString(), r["IdRol"].ToString()))
                    .ToList();

                Estatus = _logica.ConsultarEstatus().AsEnumerable()
                    .Select(r => new SelectListItem(r["Descripcion"].ToString(), r["IdEstatus"].ToString()))
                    .ToList();

                var filas = _logica.Consultar(BusquedaCuenta, BusquedaIdRol, BusquedaIdEstatus).AsEnumerable().ToList();
                TotalRegistros = filas.Count;
                TotalPaginas = Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)TamanioPagina));
                Pagina = Math.Clamp(Pagina, 1, TotalPaginas);
                Usuarios = filas.Skip((Pagina - 1) * TamanioPagina).Take(TamanioPagina).ToList();
            }
            catch (SqlException)
            {
                MostrarMensaje("No se pudo conectar con la base de datos. Intenta más tarde.", "danger");
            }
        }

        #endregion

        // Al abrir la página o al buscar
        public void OnGet()
        {
            // Mensaje que dejó un guardar/eliminar antes del redirect
            Mensaje = TempData["Mensaje"] as string ?? "";
            TipoMensaje = TempData["TipoMensaje"] as string ?? "";

            CargarPantalla();
        }

        // Botón Editar: carga el usuario en el formulario
        public void OnGetEditar(int id)
        {
            CargarPantalla();

            bdUsuario? usuario = _logica.Obtener(id);
            if (usuario != null)
                Usuario = usuario;
            else
                MostrarMensaje("No se encontró el usuario seleccionado.", "warning");
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            bool esNuevo = Usuario.IdUsuario == 0;

            if (esNuevo && string.IsNullOrWhiteSpace(Usuario.Contrasenia))
                ModelState.AddModelError("Usuario.Contrasenia", "Ingresa la contraseña.");

            // Evita que el administrador se quite el acceso a sí mismo
            if (!esNuevo && Usuario.IdUsuario == IdUsuarioSesion() && (Usuario.IdEstatus != 1 || Usuario.IdRol != 1))
                ModelState.AddModelError(string.Empty, "No puedes cambiar tu propio rol ni desactivar tu propio usuario.");

            if (!ModelState.IsValid)
            {
                MostrarMensaje(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "warning");
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

        // Exportar a Excel (CSV, se abre directo en Excel sin instalar paquetes)
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(BusquedaCuenta, BusquedaIdRol, BusquedaIdEstatus);

            var csv = new StringBuilder();
            csv.AppendLine("Cuenta,Empleado,Rol,Estatus,Fecha de registro");
            foreach (DataRow fila in dt.Rows)
            {
                csv.AppendLine(string.Join(",",
                    Csv(fila["Cuenta"]),
                    Csv(fila["Empleado"]),
                    Csv(fila["Rol"]),
                    Csv(fila["Estatus"]),
                    Csv(Convert.ToDateTime(fila["FechaRegistro"]).ToString("dd/MM/yyyy HH:mm"))));
            }

            // El BOM hace que Excel respete los acentos
            byte[] contenido = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(contenido, "text/csv", $"Gimnasio_Reporte_Usuarios_{DateTime.Now:dd-MM-yyyy}.csv");
        }

        // Escapa comillas y comas para el CSV
        private static string Csv(object valor)
        {
            string texto = valor == DBNull.Value ? string.Empty : valor.ToString()!;
            return "\"" + texto.Replace("\"", "\"\"") + "\"";
        }
    }
}
