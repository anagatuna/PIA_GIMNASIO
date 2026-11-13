using System.Data;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
            get { return RolSesion() == "Administrador"; }
        }

        public bool EsEntrenador
        {
            get { return RolSesion() == "Entrenador"; }
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
            StringBuilder csv = new StringBuilder();
            csv.AppendLine(string.Join(",", encabezados));

            foreach (DataRow fila in dt.Rows)
            {
                List<string> valores = new List<string>();
                foreach (string columna in columnas)
                {
                    // Las fechas van como dd/MM/yyyy HH:mm; lo demás, tal cual
                    string texto;
                    if (fila[columna] is DateTime)
                        texto = Convert.ToDateTime(fila[columna]).ToString("dd/MM/yyyy HH:mm");
                    else
                        texto = fila[columna].ToString() ?? "";
                    valores.Add("\"" + texto.Replace("\"", "\"\"") + "\"");
                }
                csv.AppendLine(string.Join(",", valores));
            }

            // El carácter ﻿ al inicio le indica a Excel que el archivo es UTF-8 (así respeta los acentos)
            byte[] archivo = Encoding.UTF8.GetBytes("﻿" + csv.ToString());
            return File(archivo, "text/csv", nombre + "_" + DateTime.Now.ToString("dd-MM-yyyy") + ".csv");
        }
    }
}
