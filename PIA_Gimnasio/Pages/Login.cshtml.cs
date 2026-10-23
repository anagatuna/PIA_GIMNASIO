using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Datos;

namespace PIA_Gimnasio.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly bdConexion _bd;

        public LoginModel(bdConexion bd)
        {
            _bd = bd;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? MensajeError { get; set; }

        public class LoginInput
        {
            [Required(ErrorMessage = "Ingresa tu cuenta.")]
            [StringLength(50)]
            public string Cuenta { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ingresa tu contraseña.")]
            [StringLength(50)]
            [DataType(DataType.Password)]
            public string Contrasenia { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            // Si ya inició sesión no tiene caso mostrar el login
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Index");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            SqlParameter[] parametros =
            {
                new SqlParameter("@Accion", SqlDbType.Int)               { Value = 1 },   // 1 = Login
                new SqlParameter("@Cuenta", SqlDbType.NVarChar, 50)      { Value = Input.Cuenta.Trim() },
                new SqlParameter("@Contrasenia", SqlDbType.NVarChar, 50) { Value = Input.Contrasenia }
            };

            DataTable dt;
            try
            {
                dt = _bd.EjecutarStoredProcedure("spUsuario", parametros);
            }
            catch (SqlException)
            {
                MensajeError = "No se pudo conectar con la base de datos. Intenta más tarde.";
                return Page();
            }

            if (dt.Rows.Count == 0)
            {
                MensajeError = "Cuenta o contraseña incorrectas.";
                return Page();
            }

            DataRow fila = dt.Rows[0];

            // IdEstatus 1 = Activo
            if (Convert.ToInt32(fila["IdEstatus"]) != 1)
            {
                MensajeError = $"Tu usuario está {fila["Estatus"]}. Contacta al administrador.";
                return Page();
            }

            string nombreCompleto = string.Join(" ", new[]
            {
                Texto(fila, "Empleado"),
                Texto(fila, "PrimerApellido"),
                Texto(fila, "SegundoApellido")
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, fila["IdUsuario"].ToString()!),
                new Claim(ClaimTypes.Name,           Texto(fila, "Cuenta")),
                new Claim(ClaimTypes.Role,           Texto(fila, "Rol")),
                new Claim("IdRol",                   fila["IdRol"].ToString()!),
                new Claim("IdEmpleado",              Texto(fila, "IdEmpleado")),
                new Claim("IdSucursal",              Texto(fila, "IdSucursal")),
                new Claim("NombreCompleto",          string.IsNullOrWhiteSpace(nombreCompleto) ? Texto(fila, "Cuenta") : nombreCompleto)
            };

            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad));

            // Solo redirige a URLs locales (evita open redirect)
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            return RedirectToPage("/Index");
        }

        // Convierte DBNull a "" (el empleado puede venir NULL por el LEFT JOIN)
        private static string Texto(DataRow fila, string columna)
        {
            return fila[columna] == DBNull.Value ? string.Empty : fila[columna].ToString()!;
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }
    }
}