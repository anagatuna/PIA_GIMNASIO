using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Control de acceso para recepción: entradas, salidas y aforo de la sucursal.
    // Administrador y Recepcionista.
    public class ControlAccesoModel : PaginaProtegida
    {
        private readonly logicaControlAcceso _logica;
        private readonly logicaSucursal _logicaSucursal;

        public ControlAccesoModel(logicaControlAcceso logica, logicaSucursal logicaSucursal)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            RolesPermitidos = new string[] { "ADMINISTRADOR", "RECEPCIONISTA" };
        }

        [BindProperty(SupportsGet = true)]
        public int? IdSucursal { get; set; }

        [BindProperty]
        public string? Folio { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public DataRow? Aforo { get; set; }
        public List<DataRow> Movimientos { get; set; } = new List<DataRow>();

        // Semáforo del último folio capturado
        public string Resultado { get; set; } = "";       // Permitido, Salida o Denegado ("" = esperando folio)
        public string Aviso { get; set; } = "";            // Texto que explica el resultado
        public DataRow? DatosSocio { get; set; }           // Lo que regresa el SP (socio, membresía, minutos)
        public int FolioCapturado { get; set; }

        public void OnGet()
        {
            Cargar();
        }

        // Botón Registrar entrada (también con Enter): valida membresía vigente y aforo
        public IActionResult OnPostEntrada()
        {
            Registrar(true);
            Cargar();
            return Page();
        }

        // Botón Registrar salida: el SP calcula los minutos de permanencia
        public IActionResult OnPostSalida()
        {
            Registrar(false);
            Cargar();
            return Page();
        }

        // La recepcionista solo opera su propia sucursal; el administrador puede elegir otra
        private int? SucursalPermitida()
        {
            if (EsAdministrador && IdSucursal != null)
                return IdSucursal;
            return IdSucursalSesion();
        }

        private void Registrar(bool esEntrada)
        {
            Resultado = "Denegado";
            int? sucursal = SucursalPermitida();
            if (sucursal == null)
            {
                Aviso = "No tienes una sucursal asignada.";
                return;
            }

            int numeroFolio;
            if (!int.TryParse(Folio, out numeroFolio) || numeroFolio <= 0)
            {
                Aviso = "Captura un folio válido (solo números).";
                return;
            }
            FolioCapturado = numeroFolio;

            try
            {
                // Fuera del horario de la sucursal no se registran entradas ni salidas (el SP también lo valida).
                // Las salidas pendientes se registran solas a la hora de cierre.
                DataRow? datos = _logica.ObtenerAforo(sucursal.Value);
                if (datos == null)
                {
                    Aviso = "La sucursal no está activa.";
                    return;
                }

                TimeSpan abre = (TimeSpan)datos["HoraApertura"];
                TimeSpan cierra = (TimeSpan)datos["HoraCierre"];
                TimeSpan ahora = DateTime.Now.TimeOfDay;
                if (ahora < abre || ahora > cierra)
                {
                    Aviso = "La sucursal está cerrada. Horario: " + abre.ToString(@"hh\:mm") + " - " + cierra.ToString(@"hh\:mm") + ".";
                    if (!esEntrada)
                        Aviso = Aviso + " Las salidas pendientes se registraron a la hora de cierre.";
                    return;
                }

                if (esEntrada)
                    DatosSocio = _logica.RegistrarEntrada(sucursal.Value, numeroFolio, IdUsuarioSesion());
                else
                    DatosSocio = _logica.RegistrarSalida(sucursal.Value, numeroFolio, IdUsuarioSesion());

                Resultado = DatosSocio["Resultado"].ToString() ?? "";
                Aviso = DatosSocio["Mensaje"].ToString() ?? "";
            }
            catch (Exception)
            {
                Aviso = "No se pudo conectar con la base de datos.";
            }
        }

        // Llena el combo de sucursales, el aforo y los movimientos de hoy
        private void Cargar()
        {
            IdSucursal = SucursalPermitida();

            // El campo del folio queda vacío para capturar el siguiente
            ModelState.Clear();
            Folio = "";

            try
            {
                if (EsAdministrador)
                    Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");

                if (IdSucursal != null)
                {
                    Aforo = _logica.ObtenerAforo(IdSucursal.Value);
                    if (Aforo != null)
                    {
                        foreach (DataRow r in _logica.ConsultarMovimientos(IdSucursal.Value).Rows)
                            Movimientos.Add(r);
                    }
                }

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
    }
}
