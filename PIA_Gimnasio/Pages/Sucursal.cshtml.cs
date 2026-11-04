using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Logica.Validacion;

namespace PIA_Gimnasio.Pages
{
    // Gestión de sucursales. Permisos: Seguridad/Modulos.cs (solo Administrador)
    public class SucursalModel : PaginaBase
    {
        private readonly logicaSucursal _logica;
        private readonly logicaEjercicio _logicaEstatus;   // Reutiliza el combo Activo / Inactivo

        public SucursalModel(logicaSucursal logica, logicaEjercicio logicaEstatus)
        {
            _logica = logica;
            _logicaEstatus = logicaEstatus;
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

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEstatus { get; set; }

        public List<SelectListItem> Estados { get; set; } = new();
        public List<SelectListItem> Estatus { get; set; } = new();
        public List<DataRow> Sucursales { get; set; } = new();

        public override Dictionary<string, string> RutaFiltros(int? pagina = null)
        {
            return Filtros(pagina, ("BusquedaNombre", BusquedaNombre), ("BusquedaIdEstado", BusquedaIdEstado), ("BusquedaIdEstatus", BusquedaIdEstatus));
        }

        private void CargarPantalla()
        {
            try
            {
                Estados = CargarCombo(_logica.ConsultarEstados(), "IdEstado", "Descripcion");
                Estatus = CargarCombo(_logicaEstatus.ConsultarEstatus(), "IdEstatus", "Descripcion");
                Sucursales = Paginar(_logica.Consultar(BusquedaNombre, BusquedaIdEstado, BusquedaIdEstatus));
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

            bdSucursal? sucursal = _logica.Obtener(id);
            if (sucursal != null)
                Sucursal = sucursal;
            else
                MostrarMensaje("No se encontró la sucursal seleccionada.", "warning");
        }

        public IActionResult OnPostGuardar()
        {
            List<string> errores = validacionSucursal.Validar(Sucursal);
            if (errores.Count > 0)
            {
                MostrarMensaje(errores[0], "warning");
                CargarPantalla();
                return Page();
            }

            try
            {
                MostrarRespuesta(_logica.Guardar(Sucursal, IdUsuarioSesion()));
            }
            catch (SqlException)
            {
                MostrarMensaje("Ocurrió un error al guardar la sucursal.", "danger");
            }

            if (TipoMensaje != "success")
            {
                CargarPantalla();
                return Page();
            }

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
                MostrarMensaje("Ocurrió un error al eliminar la sucursal.", "danger");
            }

            return Redirigir();
        }

        public IActionResult OnGetExportar()
        {
            return ExportarCsv(_logica.Consultar(BusquedaNombre, BusquedaIdEstado, BusquedaIdEstatus), "Sucursales",
                ("Sucursal", "Nombre"), ("Dirección", "Direccion"), ("Teléfono", "Telefono"), ("Aforo máximo", "AforoMaximo"),
                ("Ocupación actual", "Ocupacion"), ("Apertura", "HoraApertura"), ("Cierre", "HoraCierre"),
                ("Empleados", "TotalEmpleados"), ("Estatus", "Estatus"));
        }
    }
}
