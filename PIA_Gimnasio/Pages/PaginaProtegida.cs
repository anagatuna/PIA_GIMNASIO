using System.Data;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PIA_Gimnasio.Pages
{
    // Las páginas que hereden de esta clase solo se abren con sesión iniciada
    // y, si la página lo indica, solo con ciertos roles.
    public class PaginaProtegida : PageModel
    {
        // Roles que pueden abrir la página (se llena en el constructor de cada página).
        // Si se deja vacío, cualquier usuario con sesión puede entrar.
        protected string[] RolesPermitidos = { };

        // Mensaje que se muestra en la ventana de aviso (success, warning, danger)
        public string Mensaje { get; set; } = "";
        public string TipoMensaje { get; set; } = "";

        // Página a la que regresa al dar Aceptar en el aviso (vacío = se queda en la misma)
        public string RegresarA { get; set; } = "";

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            // Si no hay IdUsuario en la sesión, lo manda al Login
            if (HttpContext.Session.GetInt32("IdUsuario") == null)
            {
                context.Result = RedirectToPage("/Login");
                return;
            }

            // Si la página tiene roles, revisa que el del usuario esté en la lista
            if (RolesPermitidos.Length > 0)
            {
                bool permitido = false;
                foreach (string rol in RolesPermitidos)
                {
                    if (rol == RolSesion())
                        permitido = true;
                }

                if (!permitido)
                    context.Result = RedirectToPage("/AccesoDenegado");
            }

            // Si llegó un valor con formato inválido (ej. letras en un número o una fecha),
            // se descarta: el campo queda vacío y las validaciones de la página avisan.
            List<string> invalidos = new List<string>();
            foreach (string clave in ModelState.Keys)
            {
                ModelStateEntry? campo = ModelState[clave];
                if (campo != null && campo.Errors.Count > 0)
                    invalidos.Add(clave);
            }
            foreach (string clave in invalidos)
            {
                ModelState.Remove(clave);
            }
        }

        // Fechas fuera del rango 1900 - 2100 se toman como vacías
        // (evita errores al calcular semanas o vencimientos y al mandarlas a SQL Server)
        protected DateTime? FechaValida(DateTime? fecha)
        {
            if (fecha == null)
                return null;
            if (fecha.Value.Year < 1900 || fecha.Value.Year > 2100)
                return null;
            return fecha;
        }

        // ---------- Validaciones de formato ----------

        // true si el texto tiene exactamente "largo" caracteres y todos son números (teléfono, código postal)
        protected bool SoloNumeros(string texto, int largo)
        {
            if (texto.Length != largo)
                return false;
            foreach (char c in texto)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }

        // Correo con la forma algo@algo.algo, sin espacios y con una sola @
        protected bool CorreoValido(string correo)
        {
            if (correo.Contains(" "))
                return false;

            int arroba = correo.IndexOf("@");
            if (arroba < 1 || arroba != correo.LastIndexOf("@"))
                return false;

            // Después de la @ debe haber un punto con texto antes y después
            string dominio = correo.Substring(arroba + 1);
            int punto = dominio.LastIndexOf(".");
            return punto > 0 && punto < dominio.Length - 1;
        }

        // Revisa el texto contra un formato, caracter por caracter:
        // L = letra (A-Z, Ñ o &), N = número (0-9), A = letra o número
        protected bool CumpleFormato(string texto, string formato)
        {
            if (texto.Length != formato.Length)
                return false;

            for (int i = 0; i < texto.Length; i++)
            {
                char c = texto[i];
                bool letra = (c >= 'A' && c <= 'Z') || c == 'Ñ' || c == '&';
                bool numero = c >= '0' && c <= '9';

                if (formato[i] == 'L' && !letra)
                    return false;
                if (formato[i] == 'N' && !numero)
                    return false;
                if (formato[i] == 'A' && !letra && !numero)
                    return false;
            }
            return true;
        }

        // ---------- Datos de la sesión ----------

        protected int IdUsuarioSesion()
        {
            return HttpContext.Session.GetInt32("IdUsuario") ?? 0;
        }

        // Puede ser null si la cuenta no tiene empleado ligado
        protected int? IdEmpleadoSesion()
        {
            return HttpContext.Session.GetInt32("IdEmpleado");
        }

        protected int? IdSucursalSesion()
        {
            return HttpContext.Session.GetInt32("IdSucursal");
        }

        protected string RolSesion()
        {
            return HttpContext.Session.GetString("Rol") ?? "";
        }

        public bool EsAdministrador
        {
            get { return RolSesion() == "ADMINISTRADOR"; }
        }

        public bool EsEntrenador
        {
            get { return RolSesion() == "ENTRENADOR"; }
        }

        // La recepcionista solo trabaja con su sucursal asignada
        public bool EsRecepcionista
        {
            get { return RolSesion() == "RECEPCIONISTA"; }
        }

        // ---------- Combos ----------

        // Llena un combo con lo que regresa un SP
        protected List<SelectListItem> CargarCombo(DataTable dt, string columnaValor, string columnaTexto)
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new SelectListItem
                {
                    Value = r[columnaValor].ToString(),
                    Text = r[columnaTexto].ToString()
                });
            }
            return lista;
        }

        // Llena un combo con una lista fija (objetivos, días, tipos de sangre...)
        protected List<SelectListItem> CargarCombo(List<string> valores)
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            foreach (string valor in valores)
            {
                lista.Add(new SelectListItem { Value = valor, Text = valor });
            }
            return lista;
        }

        // ---------- Paginación de las tablas ----------

        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalRegistros { get; set; }

        const int RegistrosPorPagina = 10;

        // Regresa solo los renglones de la página actual
        protected List<DataRow> Paginar(DataTable dt)
        {
            TotalRegistros = dt.Rows.Count;
            TotalPaginas = (TotalRegistros + RegistrosPorPagina - 1) / RegistrosPorPagina;
            if (TotalPaginas == 0)
                TotalPaginas = 1;

            if (Pagina < 1)
                Pagina = 1;
            if (Pagina > TotalPaginas)
                Pagina = TotalPaginas;

            List<DataRow> filas = new List<DataRow>();
            int inicio = (Pagina - 1) * RegistrosPorPagina;
            for (int i = inicio; i < inicio + RegistrosPorPagina && i < TotalRegistros; i++)
            {
                filas.Add(dt.Rows[i]);
            }
            return filas;
        }

        // ---------- Exportar a Excel ----------

        // Arma un archivo CSV (se abre con Excel) con las columnas indicadas
        protected IActionResult ExportarExcel(DataTable dt, string nombre, string[] encabezados, string[] columnas)
        {
            // Primer renglón: los encabezados
            string csv = string.Join(",", encabezados) + "\r\n";

            foreach (DataRow fila in dt.Rows)
            {
                List<string> valores = new List<string>();
                foreach (string columna in columnas)
                {
                    // Las fechas van como dd/MM/yyyy HH:mm; lo demás, tal cual
                    string texto;
                    if (fila[columna].GetType() == typeof(DateTime))
                        texto = Convert.ToDateTime(fila[columna]).ToString("dd/MM/yyyy HH:mm");
                    else
                        texto = fila[columna].ToString() ?? "";
                    valores.Add("\"" + texto.Replace("\"", "\"\"") + "\"");
                }
                csv = csv + string.Join(",", valores) + "\r\n";
            }

            // El carácter ﻿ al inicio le indica a Excel que el archivo es UTF-8 (así respeta los acentos)
            byte[] archivo = Encoding.UTF8.GetBytes("﻿" + csv);
            return File(archivo, "text/csv", nombre + "_" + DateTime.Now.ToString("dd-MM-yyyy") + ".csv");
        }
    }
}
