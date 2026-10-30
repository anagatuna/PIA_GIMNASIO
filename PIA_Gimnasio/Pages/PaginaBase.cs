using System.Data;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Seguridad;

namespace PIA_Gimnasio.Pages
{
    // Clase base de las pantallas (equivalente al code-behind del Master en Web Forms).
    // Cada página hereda de aquí: public class UsuarioModel : PaginaBase
    public abstract class PaginaBase : PageModel
    {
        protected const int TamanioPagina = 10;
        protected const string ErrorConexion = "No se pudo conectar con la base de datos. Intenta más tarde.";

        #region Mensajes

        // [ViewData]: el Layout los lee para mostrar el aviso
        [ViewData] public string Mensaje { get; set; } = "";
        [ViewData] public string TipoMensaje { get; set; } = "";     // success, warning, danger, info

        // Muestra un aviso en la pantalla
        protected void MostrarMensaje(string mensaje, string tipo)
        {
            Mensaje = mensaje;
            TipoMensaje = tipo;
        }

        // Muestra el mensaje que regresó el SP
        protected void MostrarRespuesta(bdRespuesta respuesta)
        {
            MostrarMensaje(respuesta.Mensaje, respuesta.TipoMensaje);
        }

        protected void MostrarErrorConexion()
        {
            MostrarMensaje(ErrorConexion, "danger");
        }

        // Antes de cada handler: recupera el aviso que dejó un Redirigir() (guardar, eliminar)
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            if (TempData["Mensaje"] is string mensaje)
                MostrarMensaje(mensaje, TempData["TipoMensaje"] as string ?? "info");
        }

        #endregion

        #region Sesión

        // Id del usuario que inició sesión (para la auditoría)
        protected int IdUsuarioSesion()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : 0;
        }

        // Empleado ligado al usuario (null si la cuenta no tiene empleado)
        protected int? IdEmpleadoSesion()
        {
            return int.TryParse(User.FindFirstValue("IdEmpleado"), out int id) ? id : null;
        }

        public bool EsEntrenador => User.IsInRole(Roles.Entrenador);

        #endregion

        #region Redirección con filtros

        // Filtros de búsqueda actuales; cada página los define para conservarlos al paginar, guardar o eliminar
        public virtual Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina);
        }

        // Arma la ruta con los filtros que tienen valor: Filtros(pagina, ("BusquedaCuenta", BusquedaCuenta), ...)
        protected Dictionary<string, string> Filtros(int? pagina, params (string Nombre, object? Valor)[] valores)
        {
            var ruta = new Dictionary<string, string>();
            foreach (var (nombre, valor) in valores)
            {
                string? texto = valor?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(texto))
                    ruta[nombre] = texto;
            }
            ruta["Pagina"] = (pagina ?? Pagina).ToString();
            return ruta;
        }

        // Regresa a la misma página y búsqueda, conservando el aviso (patrón guardar -> redirect)
        protected IActionResult Redirigir()
        {
            return Redirigir(null, RutaFiltros());
        }

        // Va a otra página conservando el aviso, ej. Redirigir("/RutinaDetalle", new { id = 5 })
        protected IActionResult Redirigir(string? pagina, object? ruta = null)
        {
            TempData["Mensaje"] = Mensaje;
            TempData["TipoMensaje"] = TipoMensaje;
            return RedirectToPage(pagina, ruta);
        }

        #endregion

        #region Tablas y combos

        // Paginación de las tablas (equivalente a CargarGridView): <partial name="_Paginacion" model="Model" />
        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 1;
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; } = 1;

        // Regresa solo los renglones de la página actual
        protected List<DataRow> Paginar(DataTable tabla)
        {
            TotalRegistros = tabla.Rows.Count;
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)TamanioPagina));
            Pagina = Math.Clamp(Pagina, 1, TotalPaginas);
            return tabla.AsEnumerable().Skip((Pagina - 1) * TamanioPagina).Take(TamanioPagina).ToList();
        }

        // Llena un combo con lo que regresa un SP (equivalente a CargarDropDownList)
        protected static List<SelectListItem> CargarCombo(DataTable tabla, string columnaValor, string columnaTexto)
        {
            return tabla.AsEnumerable()
                .Select(r => new SelectListItem(r[columnaTexto].ToString(), r[columnaValor].ToString()))
                .ToList();
        }

        // Llena un combo con una lista fija (niveles, días, objetivos)
        protected static List<SelectListItem> CargarCombo(IEnumerable<string> valores)
        {
            return valores.Select(v => new SelectListItem(v, v)).ToList();
        }

        // Exporta una consulta a CSV para abrir en Excel: ExportarCsv(tabla, "Usuarios", ("Cuenta", "Cuenta"), ...)
        protected FileContentResult ExportarCsv(DataTable tabla, string nombre, params (string Encabezado, string Columna)[] columnas)
        {
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",", columnas.Select(c => Csv(c.Encabezado))));
            foreach (DataRow fila in tabla.Rows)
                csv.AppendLine(string.Join(",", columnas.Select(c => Csv(fila[c.Columna]))));

            // El BOM hace que Excel respete los acentos
            byte[] contenido = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(contenido, "text/csv", $"Gimnasio_Reporte_{nombre}_{DateTime.Now:dd-MM-yyyy}.csv");
        }

        // Escapa comillas y comas; las fechas van como dd/MM/yyyy HH:mm
        private static string Csv(object valor)
        {
            string texto = valor switch
            {
                DBNull => "",
                DateTime fecha => fecha.ToString("dd/MM/yyyy HH:mm"),
                _ => valor.ToString() ?? ""
            };
            return "\"" + texto.Replace("\"", "\"\"") + "\"";
        }

        #endregion
    }
}
