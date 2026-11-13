using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Control de acceso para recepción: entradas, salidas y aforo de la sucursal.
    // La entrada/salida se registra con JavaScript (sin recargar) y el aforo se refresca cada 5 segundos.
    // Administrador y Recepcionista.
    public class ControlAccesoModel : PaginaProtegida
    {
        private readonly logicaControlAcceso _logica;
        private readonly logicaSucursal _logicaSucursal;

        public ControlAccesoModel(logicaControlAcceso logica, logicaSucursal logicaSucursal)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            RolesPermitidos = new string[] { "Administrador", "Recepcionista" };
        }

        [BindProperty(SupportsGet = true)]
        public int? IdSucursal { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public DataRow? Aforo { get; set; }

        // La recepcionista solo opera su propia sucursal; el administrador puede elegir otra
        private int? SucursalPermitida(int? solicitada)
        {
            if (EsAdministrador && solicitada != null)
                return solicitada;
            return IdSucursalSesion();
        }

        public void OnGet()
        {
            IdSucursal = SucursalPermitida(IdSucursal);

            try
            {
                if (EsAdministrador)
                    Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");

                if (IdSucursal != null)
                    Aforo = _logica.ObtenerAforo(IdSucursal.Value);

                if (Aforo == null)
                {
                    Mensaje = "Tu usuario no tiene una sucursal activa asignada.";
                    TipoMensaje = "warning";
                }
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // Aforo y movimientos del día (lo consulta el JavaScript cada 5 segundos)
        public IActionResult OnGetEstado(int? idSucursal)
        {
            int? sucursal = SucursalPermitida(idSucursal);
            if (sucursal == null)
                return BadRequest();

            try
            {
                DataRow? aforo = _logica.ObtenerAforo(sucursal.Value);
                if (aforo == null)
                    return NotFound();

                // Movimientos del día para la tabla
                List<object> movimientos = new List<object>();
                foreach (DataRow r in _logica.ConsultarMovimientos(sucursal.Value).Rows)
                {
                    movimientos.Add(new
                    {
                        folio = r["Folio"],
                        socio = r["Socio"],
                        entrada = Convert.ToDateTime(r["FechaEntrada"]).ToString("HH:mm"),
                        salida = r["FechaSalida"] == DBNull.Value ? null : Convert.ToDateTime(r["FechaSalida"]).ToString("HH:mm"),
                        minutos = Convert.ToInt32(r["Minutos"])
                    });
                }

                return new JsonResult(new
                {
                    ocupacion = Convert.ToInt32(aforo["Ocupacion"]),
                    aforoMaximo = Convert.ToInt32(aforo["AforoMaximo"]),
                    movimientos = movimientos
                });
            }
            catch (Exception)
            {
                return StatusCode(503);
            }
        }

        // Registrar entrada: valida membresía vigente y aforo
        public IActionResult OnPostEntrada(int? idSucursal, string? folio)
        {
            return Registrar(idSucursal, folio, true);
        }

        // Registrar salida: el SP calcula los minutos de permanencia
        public IActionResult OnPostSalida(int? idSucursal, string? folio)
        {
            return Registrar(idSucursal, folio, false);
        }

        private IActionResult Registrar(int? idSucursal, string? folio, bool esEntrada)
        {
            int? sucursal = SucursalPermitida(idSucursal);
            if (sucursal == null)
                return new JsonResult(new { resultado = "Denegado", mensaje = "No tienes una sucursal asignada." });

            int numeroFolio;
            if (!int.TryParse(folio?.Trim(), out numeroFolio) || numeroFolio <= 0)
                return new JsonResult(new { resultado = "Denegado", mensaje = "Captura un folio válido (solo números)." });

            try
            {
                DataRow r;
                if (esEntrada)
                    r = _logica.RegistrarEntrada(sucursal.Value, numeroFolio, IdUsuarioSesion());
                else
                    r = _logica.RegistrarSalida(sucursal.Value, numeroFolio, IdUsuarioSesion());

                return new JsonResult(new
                {
                    resultado = r["Resultado"].ToString(),
                    mensaje = r["Mensaje"].ToString(),
                    folio = numeroFolio,
                    socio = r["Socio"] == DBNull.Value ? null : r["Socio"].ToString(),
                    membresia = r["Membresia"] == DBNull.Value ? null : r["Membresia"].ToString(),
                    vence = r["FechaVencimiento"] == DBNull.Value ? null : Convert.ToDateTime(r["FechaVencimiento"]).ToString("dd/MM/yyyy"),
                    minutos = r["Minutos"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Minutos"]),
                    ocupacion = Convert.ToInt32(r["Ocupacion"]),
                    aforoMaximo = Convert.ToInt32(r["AforoMaximo"])
                });
            }
            catch (Exception)
            {
                return new JsonResult(new { resultado = "Denegado", mensaje = "No se pudo conectar con la base de datos." });
            }
        }
    }
}
