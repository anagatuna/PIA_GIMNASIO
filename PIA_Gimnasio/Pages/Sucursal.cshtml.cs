using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Gestión de sucursales (solo Administrador)
    public class SucursalModel : PaginaProtegida
    {
        private readonly logicaSucursal _logica;

        public SucursalModel(logicaSucursal logica)
        {
            _logica = logica;
            RolesPermitidos = new string[] { "Administrador" };
        }

        [BindProperty]
        public bdSucursal Sucursal { get; set; } = new bdSucursal
        {
            HoraApertura = new TimeSpan(6, 0, 0),
            HoraCierre = new TimeSpan(22, 0, 0)
        };

        [BindProperty(SupportsGet = true)]
        public string? BusquedaNombre { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstado { get; set; }

        public List<SelectListItem> Estados { get; set; } = new List<SelectListItem>();
        public List<DataRow> Sucursales { get; set; } = new List<DataRow>();

        public void OnGet()
        {
            Cargar();
        }

        // Botón Editar: carga la sucursal en el formulario
        public void OnGetEditar(int id)
        {
            try
            {
                Sucursal = _logica.Obtener(id) ?? new bdSucursal();
            }
            catch (Exception)
            {
                Mensaje = "No se pudo cargar la sucursal.";
                TipoMensaje = "danger";
            }
            Cargar();
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            // El estatus no se captura: siempre Activa (solo pasa a Inactiva con la baja lógica)
            Sucursal.IdEstatus = 1;

            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                Cargar();
                return Page();
            }

            try
            {
                bdRespuesta r = _logica.Guardar(Sucursal, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                if (r.TipoMensaje == "success")
                    Limpiar();
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar la sucursal.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Eliminar (baja lógica)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                bdRespuesta r = _logica.Eliminar(id, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al eliminar la sucursal.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Exportar a Excel
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(BusquedaNombre, BusquedaIdEstado, null);
            return ExportarExcel(dt, "Sucursales",
                new string[] { "Sucursal", "Dirección", "Teléfono", "Aforo máximo", "Ocupación actual", "Apertura", "Cierre", "Empleados" },
                new string[] { "Nombre", "Direccion", "Telefono", "AforoMaximo", "Ocupacion", "HoraApertura", "HoraCierre", "TotalEmpleados" });
        }

        // Llena combos y tabla
        private void Cargar()
        {
            try
            {
                Estados = CargarCombo(_logica.ConsultarEstados(), "IdEstado", "Descripcion");
                Sucursales = Paginar(_logica.Consultar(BusquedaNombre, BusquedaIdEstado, null));
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
            if (string.IsNullOrWhiteSpace(Sucursal.Nombre))
                return "Ingresa el nombre de la sucursal.";
            if (string.IsNullOrWhiteSpace(Sucursal.Telefono) || !Regex.IsMatch(Sucursal.Telefono.Trim(), @"^\d{10}$"))
                return "El teléfono debe tener 10 dígitos.";
            if (Sucursal.AforoMaximo == null || Sucursal.AforoMaximo < 1 || Sucursal.AforoMaximo > 5000)
                return "El aforo máximo debe estar entre 1 y 5000 personas.";
            if (Sucursal.HoraApertura == null || Sucursal.HoraCierre == null)
                return "Ingresa el horario de apertura y cierre.";
            if (Sucursal.HoraCierre <= Sucursal.HoraApertura)
                return "La hora de cierre debe ser posterior a la hora de apertura.";
            if (string.IsNullOrWhiteSpace(Sucursal.Calle) || string.IsNullOrWhiteSpace(Sucursal.NumeroInt) ||
                string.IsNullOrWhiteSpace(Sucursal.Colonia) || string.IsNullOrWhiteSpace(Sucursal.Municipio))
                return "Ingresa la dirección completa (calle, número, colonia y municipio).";
            if (Sucursal.IdEstado == null)
                return "Selecciona el estado.";
            if (string.IsNullOrWhiteSpace(Sucursal.CodigoPostal) || !Regex.IsMatch(Sucursal.CodigoPostal.Trim(), @"^\d{5}$"))
                return "El código postal debe tener 5 dígitos.";
            return "";
        }

        private void Limpiar()
        {
            ModelState.Clear();
            Sucursal = new bdSucursal
            {
                HoraApertura = new TimeSpan(6, 0, 0),
                HoraCierre = new TimeSpan(22, 0, 0)
            };
        }
    }
}
