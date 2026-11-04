using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;
using PIA_Gimnasio.Seguridad;

namespace PIA_Gimnasio.Pages
{
    // Pantalla de alta velocidad para recepción: entradas, salidas y aforo en tiempo real.
    // La entrada/salida se registra con fetch (sin recargar) y el aforo se refresca cada 5 segundos.
    // Permisos: Seguridad/Modulos.cs (Administrador y Recepcionista)
    public class ControlAccesoModel : PaginaBase
    {
        private readonly logicaControlAcceso _logica;
        private readonly logicaSucursal _logicaSucursal;

        public ControlAccesoModel(logicaControlAcceso logica, logicaSucursal logicaSucursal)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
        }

        [BindProperty(SupportsGet = true)]
        public int? IdSucursal { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new();
        public DataRow? Aforo { get; set; }
        public bool EsAdministrador => User.IsInRole(Roles.Administrador);

        // La recepcionista solo opera su propia sucursal; el administrador puede elegir
        private int? SucursalPermitida(int? solicitada)
        {
            int? propia = int.TryParse(User.FindFirst("IdSucursal")?.Value, out int id) ? id : null;
            return EsAdministrador ? (solicitada ?? propia) : propia;
        }

        public void OnGet()
        {
            IdSucursal = SucursalPermitida(IdSucursal);

            try
            {
                if (EsAdministrador)
                    Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");

                if (IdSucursal.HasValue)
                    Aforo = _logica.ObtenerAforo(IdSucursal.Value);

                if (Aforo == null)
                    MostrarMensaje("Tu usuario no tiene una sucursal activa asignada.", "warning");
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }
        }

        // Aforo y movimientos del día (lo consulta el JavaScript cada 5 segundos)
        public IActionResult OnGetEstado(int? idSucursal)
        {
            int? sucursal = SucursalPermitida(idSucursal);
            if (!sucursal.HasValue)
                return BadRequest();

            try
            {
                DataRow? aforo = _logica.ObtenerAforo(sucursal.Value);
                if (aforo == null)
                    return NotFound();

                return new JsonResult(new
                {
                    ocupacion = Convert.ToInt32(aforo["Ocupacion"]),
                    aforoMaximo = Convert.ToInt32(aforo["AforoMaximo"]),
                    movimientos = _logica.ConsultarMovimientos(sucursal.Value).AsEnumerable().Select(r => new
                    {
                        folio = r["Folio"],
                        socio = r["Socio"],
                        entrada = Convert.ToDateTime(r["FechaEntrada"]).ToString("HH:mm"),
                        salida = r["FechaSalida"] == DBNull.Value ? null : Convert.ToDateTime(r["FechaSalida"]).ToString("HH:mm"),
                        minutos = Convert.ToInt32(r["Minutos"])
                    })
                });
            }
            catch (SqlException)
            {
                return StatusCode(503);
            }
        }

        // Registrar entrada: valida membresía vigente y aforo (semáforo verde / rojo)
        public IActionResult OnPostEntrada(int? idSucursal, string? folio)
        {
            return Registrar(idSucursal, folio, entrada: true);
        }

        // Registrar salida: el SP calcula los minutos de permanencia
        public IActionResult OnPostSalida(int? idSucursal, string? folio)
        {
            return Registrar(idSucursal, folio, entrada: false);
        }

        private IActionResult Registrar(int? idSucursal, string? folio, bool entrada)
        {
            int? sucursal = SucursalPermitida(idSucursal);
            if (!sucursal.HasValue)
                return new JsonResult(new { resultado = "Denegado", mensaje = "No tienes una sucursal asignada." });

            if (!int.TryParse(folio?.Trim(), out int numeroFolio) || numeroFolio <= 0)
                return new JsonResult(new { resultado = "Denegado", mensaje = "Captura un folio válido (solo números)." });

            try
            {
                DataRow r = entrada
                    ? _logica.RegistrarEntrada(sucursal.Value, numeroFolio, IdUsuarioSesion())
                    : _logica.RegistrarSalida(sucursal.Value, numeroFolio, IdUsuarioSesion());

                return new JsonResult(new
                {
                    resultado = r["Resultado"].ToString(),
                    mensaje = r["Mensaje"].ToString(),
                    folio = numeroFolio,
                    socio = r["Socio"] as string,
                    membresia = r["Membresia"] as string,
                    vence = r["FechaVencimiento"] == DBNull.Value ? null : Convert.ToDateTime(r["FechaVencimiento"]).ToString("dd/MM/yyyy"),
                    minutos = r["Minutos"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Minutos"]),
                    ocupacion = Convert.ToInt32(r["Ocupacion"]),
                    aforoMaximo = Convert.ToInt32(r["AforoMaximo"])
                });
            }
            catch (SqlException)
            {
                return new JsonResult(new { resultado = "Denegado", mensaje = ErrorConexion });
            }
        }
    }
}
