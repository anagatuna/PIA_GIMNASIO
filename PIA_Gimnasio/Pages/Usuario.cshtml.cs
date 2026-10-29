using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Pages
{
    // Solo el Administrador puede gestionar usuarios
    [Authorize(Roles = "Administrador")]
    public class UsuarioModel : PageModel
    {
        private const int TamanioPagina = 10;
        private readonly bdConexion _bd;

        public UsuarioModel(bdConexion bd)
        {
            _bd = bd;
        }

        // ---------- Formulario de gestión ----------
        [BindProperty]
        public UsuarioInput Input { get; set; } = new();

        public class UsuarioInput
        {
            // 0 = nuevo registro, > 0 = edición
            public int IdUsuario { get; set; }

            [Required(ErrorMessage = "Ingresa la cuenta.")]
            [StringLength(50, ErrorMessage = "La cuenta no puede pasar de 50 caracteres.")]
            [RegularExpression(@"^\S+$", ErrorMessage = "La cuenta no puede llevar espacios.")]
            public string Cuenta { get; set; } = string.Empty;

            // Obligatoria solo al crear; al editar, vacía = conservar la actual
            [StringLength(50, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 50 caracteres.")]
            [DataType(DataType.Password)]
            public string? Contrasenia { get; set; }

            [Required(ErrorMessage = "Selecciona el rol.")]
            public int? IdRol { get; set; }

            [Required(ErrorMessage = "Selecciona el estatus.")]
            public int? IdEstatus { get; set; } = 1;
        }

        // ---------- Filtros de búsqueda (viajan en la URL para conservarse al paginar) ----------
        [BindProperty(SupportsGet = true)]
        public string? BusquedaCuenta { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdRol { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 1;

        // ---------- Datos para la vista ----------
        public List<SelectListItem> Roles { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<DataRow> Registros { get; set; } = new();
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }

        public string? Mensaje { get; set; }
        public string? TipoMensaje { get; set; }

        #region Utilidades

        private void MostrarMensaje(string mensaje, string tipo)
        {
            Mensaje = mensaje;
            TipoMensaje = tipo;
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

        private DataTable EjecutarSP(int accion, int? idUsuario = null, string? cuenta = null, string? contrasenia = null, int? idRol = null, int? idEstatus = null)
        {
            SqlParameter[] parametros =
            {
                new SqlParameter("@Accion", SqlDbType.Int)               { Value = accion },
                new SqlParameter("@IdUsuario", SqlDbType.Int)            { Value = (object?)idUsuario ?? DBNull.Value },
                new SqlParameter("@Cuenta", SqlDbType.NVarChar, 50)      { Value = (object?)cuenta ?? DBNull.Value },
                new SqlParameter("@Contrasenia", SqlDbType.NVarChar, 50) { Value = (object?)contrasenia ?? DBNull.Value },
                new SqlParameter("@IdRol", SqlDbType.Int)                { Value = (object?)idRol ?? DBNull.Value },
                new SqlParameter("@IdEstatus", SqlDbType.Int)            { Value = (object?)idEstatus ?? DBNull.Value },
                new SqlParameter("@UsuarioSesion", SqlDbType.Int)        { Value = IdUsuarioSesion() }
            };
            return _bd.EjecutarStoredProcedure("spUsuario", parametros);
        }

        #endregion

        #region Carga inicial

        private void CargarCombos()
        {
            Roles = EjecutarSP(8).AsEnumerable()
                .Select(r => new SelectListItem(r["Descripcion"].ToString(), r["IdRol"].ToString()))
                .ToList();

            Estatus = EjecutarSP(9).AsEnumerable()
                .Select(r => new SelectListItem(r["Descripcion"].ToString(), r["IdEstatus"].ToString()))
                .ToList();
        }

        private DataTable ConsultarUsuarios()
        {
            string? cuenta = string.IsNullOrWhiteSpace(BusquedaCuenta) ? null : BusquedaCuenta.Trim();
            return EjecutarSP(3, cuenta: cuenta, idRol: BusquedaIdRol, idEstatus: BusquedaIdEstatus);
        }

        // Llena combos y tabla (con paginación)
        private void CargarPantalla()
        {
            try
            {
                CargarCombos();

                var filas = ConsultarUsuarios().AsEnumerable().ToList();
                TotalRegistros = filas.Count;
                TotalPaginas = Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)TamanioPagina));
                Pagina = Math.Clamp(Pagina, 1, TotalPaginas);
                Registros = filas.Skip((Pagina - 1) * TamanioPagina).Take(TamanioPagina).ToList();
            }
            catch (SqlException)
            {
                MostrarMensaje("No se pudo conectar con la base de datos. Intenta más tarde.", "error");
            }
        }

        #endregion

        // Carga la pantalla; si viene "editar" en la URL, llena el formulario con ese usuario
        public void OnGet(int? editar)
        {
            // Mensaje que dejó un guardar/eliminar antes del redirect
            Mensaje = TempData["Mensaje"] as string;
            TipoMensaje = TempData["TipoMensaje"] as string;

            CargarPantalla();

            if (editar.HasValue)
            {
                DataTable dt = EjecutarSP(4, idUsuario: editar.Value);
                if (dt.Rows.Count > 0)
                {
                    DataRow fila = dt.Rows[0];
                    Input = new UsuarioInput
                    {
                        IdUsuario = Convert.ToInt32(fila["IdUsuario"]),
                        Cuenta = fila["Cuenta"].ToString()!,
                        IdRol = Convert.ToInt32(fila["IdRol"]),
                        IdEstatus = Convert.ToInt32(fila["IdEstatus"])
                    };
                }
                else
                {
                    MostrarMensaje("No se encontró el usuario seleccionado.", "warning");
                }
            }
        }

        // Guardar y editar
        public IActionResult OnPostGuardar()
        {
            bool esNuevo = Input.IdUsuario == 0;

            if (esNuevo && string.IsNullOrWhiteSpace(Input.Contrasenia))
                ModelState.AddModelError("Input.Contrasenia", "Ingresa la contraseña.");

            // Evita que el administrador se quite el acceso a sí mismo
            if (!esNuevo && Input.IdUsuario == IdUsuarioSesion() && (Input.IdEstatus != 1 || Input.IdRol != 1))
                ModelState.AddModelError(string.Empty, "No puedes cambiar tu propio rol ni desactivar tu propio usuario.");

            if (!ModelState.IsValid)
            {
                MostrarMensaje(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                DataTable dt = EjecutarSP(esNuevo ? 5 : 6,
                    idUsuario: esNuevo ? null : Input.IdUsuario,
                    cuenta: Input.Cuenta.Trim(),
                    contrasenia: string.IsNullOrWhiteSpace(Input.Contrasenia) ? null : Input.Contrasenia,
                    idRol: Input.IdRol,
                    idEstatus: Input.IdEstatus);

                string mensaje = dt.Rows[0]["Mensaje"].ToString()!;
                string tipo = dt.Rows[0]["Tipo"].ToString()!;
                MostrarMensaje(mensaje, tipo);

                // Si no se guardó, se queda el formulario lleno para corregir
                if (tipo != "success")
                {
                    CargarPantalla();
                    return Page();
                }
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar el usuario.", "error");
                CargarPantalla();
                return Page();
            }

            return Redirigir();
        }

        // Eliminar (baja lógica)
        public IActionResult OnPostEliminar(int id)
        {
            if (id == IdUsuarioSesion())
            {
                MostrarMensaje("No puedes eliminar tu propio usuario.", "warning");
                return Redirigir();
            }

            try
            {
                DataTable dt = EjecutarSP(7, idUsuario: id);
                MostrarMensaje(dt.Rows[0]["Mensaje"].ToString()!, dt.Rows[0]["Tipo"].ToString()!);
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al eliminar el usuario.", "error");
            }

            return Redirigir();
        }

        // Exportar a Excel (CSV, se abre directo en Excel sin instalar paquetes)
        public IActionResult OnGetExportar()
        {
            DataTable dt = ConsultarUsuarios();

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
