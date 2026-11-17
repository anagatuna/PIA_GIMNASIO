using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Gestión de empleados con sus especialidades (solo Administrador)
    public class EmpleadoModel : PaginaProtegida
    {
        private readonly logicaEmpleado _logica;
        private readonly logicaSucursal _logicaSucursal;
        private readonly logicaCatalogo _logicaCatalogo;

        public EmpleadoModel(logicaEmpleado logica, logicaSucursal logicaSucursal, logicaCatalogo logicaCatalogo)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            _logicaCatalogo = logicaCatalogo;
            RolesPermitidos = new string[] { "ADMINISTRADOR" };
        }

        [BindProperty]
        public bdEmpleado Empleado { get; set; } = new bdEmpleado();

        [BindProperty(SupportsGet = true)]
        public string? BusquedaNombre { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdSucursal { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdPuesto { get; set; }

        public List<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Puestos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Especialidades { get; set; } = new List<SelectListItem>();
        public List<DataRow> Empleados { get; set; } = new List<DataRow>();

        // Id del puesto "Entrenador": el JavaScript habilita las especialidades solo con ese puesto
        public int IdPuestoEntrenador { get; set; }

        public bool EsPuestoEntrenador
        {
            get { return Empleado.IdPuesto == IdPuestoEntrenador; }
        }

        public void OnGet()
        {
            Cargar();
        }

        // Botón Editar: carga el empleado en el formulario
        public void OnGetEditar(int id)
        {
            try
            {
                Empleado = _logica.Obtener(id) ?? new bdEmpleado();
            }
            catch (Exception)
            {
                Mensaje = "No se pudo cargar el empleado.";
                TipoMensaje = "danger";
            }
            Cargar();
        }

        // Botón Guardar / Actualizar
        public IActionResult OnPostGuardar()
        {
            // Las fechas fuera de rango se toman como vacías (la validación pide capturarlas)
            Empleado.FechaNacimiento = FechaValida(Empleado.FechaNacimiento);
            Empleado.FechaContratacion = FechaValida(Empleado.FechaContratacion);

            Cargar();

            // El estatus no se captura: siempre Activo (solo pasa a Inactivo con la baja lógica)
            Empleado.IdEstatus = 1;

            // Las especialidades solo aplican al puesto Entrenador
            if (!EsPuestoEntrenador)
                Empleado.Especialidades.Clear();

            Mensaje = Validar();
            if (Mensaje != "")
            {
                TipoMensaje = "warning";
                return Page();
            }

            try
            {
                bdRespuesta r = _logica.Guardar(Empleado, IdUsuarioSesion());
                Mensaje = r.Mensaje;
                TipoMensaje = r.TipoMensaje;

                if (r.TipoMensaje == "success")
                {
                    Limpiar();
                    Cargar();
                }
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al guardar el empleado.";
                TipoMensaje = "danger";
            }
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
                Mensaje = "Ocurrió un error al eliminar el empleado.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Botón Exportar a Excel
        public IActionResult OnGetExportar()
        {
            DataTable dt = _logica.Consultar(BusquedaNombre, BusquedaIdSucursal, BusquedaIdPuesto, null);
            return ExportarExcel(dt, "Empleados",
                new string[] { "Empleado", "Puesto", "Especialidades", "Sucursal", "Correo", "Teléfono", "CURP", "RFC", "Fecha de contratación", "Cuenta de usuario" },
                new string[] { "Empleado", "Puesto", "Especialidades", "Sucursal", "Correo", "Telefono", "CURP", "RFC", "FechaContratacion", "Cuenta" });
        }

        // Llena combos y tabla
        private void Cargar()
        {
            try
            {
                Sucursales = CargarCombo(_logicaSucursal.ConsultarActivas(), "IdSucursal", "Nombre");
                Puestos = CargarCombo(_logicaCatalogo.ConsultarPuestos(), "IdCatalogo", "Descripcion");
                Especialidades = CargarCombo(_logicaCatalogo.ConsultarEspecialidades(), "IdCatalogo", "Descripcion");

                // Busca el Id del puesto Entrenador
                foreach (SelectListItem p in Puestos)
                {
                    if (p.Text.ToUpper() == "ENTRENADOR")
                        IdPuestoEntrenador = Convert.ToInt32(p.Value);
                }

                Empleados = Paginar(_logica.Consultar(BusquedaNombre, BusquedaIdSucursal, BusquedaIdPuesto, null));
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }

        // CURP: 4 letras, 6 números (fecha), H/M/X (sexo), 5 letras, 1 letra o número y 1 número
        private bool CurpValida(string curp)
        {
            if (!CumpleFormato(curp, "LLLLNNNNNNLLLLLLAN"))
                return false;
            return curp[10] == 'H' || curp[10] == 'M' || curp[10] == 'X';
        }

        // RFC: 3 letras (empresa) o 4 (persona), 6 números (fecha) y 3 letras o números (homoclave)
        private bool RfcValido(string rfc)
        {
            return CumpleFormato(rfc, "LLLNNNNNNAAA") || CumpleFormato(rfc, "LLLLNNNNNNAAA");
        }

        // Reglas de captura. Regresa el primer error o "" si todo está bien.
        private string Validar()
        {
            if (string.IsNullOrWhiteSpace(Empleado.Nombre) || string.IsNullOrWhiteSpace(Empleado.PrimerApellido))
                return "Ingresa el nombre y el primer apellido.";

            // Identificación: al menos CURP o RFC, y con formato válido si se captura
            if (string.IsNullOrWhiteSpace(Empleado.CURP) && string.IsNullOrWhiteSpace(Empleado.RFC))
                return "Captura al menos la CURP o el RFC del empleado.";
            if (!string.IsNullOrWhiteSpace(Empleado.CURP) && !CurpValida(Empleado.CURP.Trim().ToUpper()))
                return "La CURP no tiene un formato válido (18 caracteres).";
            if (!string.IsNullOrWhiteSpace(Empleado.RFC) && !RfcValido(Empleado.RFC.Trim().ToUpper()))
                return "El RFC no tiene un formato válido (12 o 13 caracteres).";

            if (Empleado.FechaNacimiento == null)
                return "Ingresa la fecha de nacimiento.";
            if (Empleado.FechaNacimiento > DateTime.Today.AddYears(-18) || Empleado.FechaNacimiento < DateTime.Today.AddYears(-90))
                return "El empleado debe tener entre 18 y 90 años.";

            if (string.IsNullOrWhiteSpace(Empleado.Correo) || !CorreoValido(Empleado.Correo.Trim()))
                return "Ingresa un correo válido.";
            if (string.IsNullOrWhiteSpace(Empleado.Telefono) || !SoloNumeros(Empleado.Telefono.Trim(), 10))
                return "El teléfono debe tener 10 dígitos.";

            if (Empleado.IdSucursal == null)
                return "Selecciona la sucursal.";
            if (Empleado.IdPuesto == null)
                return "Selecciona el puesto.";

            if (Empleado.FechaContratacion == null)
                return "Ingresa la fecha de contratación.";
            if (Empleado.FechaContratacion > DateTime.Today)
                return "La fecha de contratación no puede ser futura.";
            if (Empleado.FechaContratacion < Empleado.FechaNacimiento.Value.AddYears(18))
                return "La fecha de contratación debe ser posterior a sus 18 años.";

            // Especialidades: obligatorias para un entrenador
            if (EsPuestoEntrenador && Empleado.Especialidades.Count == 0)
                return "Selecciona al menos una especialidad para el entrenador.";


            return "";
        }

        private void Limpiar()
        {
            ModelState.Clear();
            Empleado = new bdEmpleado();
        }
    }
}
