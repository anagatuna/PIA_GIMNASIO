using System.Data;
using System.Text.RegularExpressions;
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

        public EmpleadoModel(logicaEmpleado logica, logicaSucursal logicaSucursal)
        {
            _logica = logica;
            _logicaSucursal = logicaSucursal;
            RolesPermitidos = new string[] { "Administrador" };
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
                Puestos = CargarCombo(_logica.ConsultarPuestos(), "IdPuesto", "Nombre");
                Especialidades = CargarCombo(_logica.ConsultarEspecialidades(), "IdEspecialidad", "Nombre");

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

        // Reglas de captura. Regresa el primer error o "" si todo está bien.
        private string Validar()
        {
            if (string.IsNullOrWhiteSpace(Empleado.Nombre) || string.IsNullOrWhiteSpace(Empleado.PrimerApellido))
                return "Ingresa el nombre y el primer apellido.";

            // Identificación: al menos CURP o RFC, y con formato válido si se captura
            if (string.IsNullOrWhiteSpace(Empleado.CURP) && string.IsNullOrWhiteSpace(Empleado.RFC))
                return "Captura al menos la CURP o el RFC del empleado.";
            if (!string.IsNullOrWhiteSpace(Empleado.CURP) && !Regex.IsMatch(Empleado.CURP.Trim().ToUpper(), @"^[A-Z]{4}\d{6}[HMX][A-Z]{5}[A-Z0-9]\d$"))
                return "La CURP no tiene un formato válido (18 caracteres).";
            if (!string.IsNullOrWhiteSpace(Empleado.RFC) && !Regex.IsMatch(Empleado.RFC.Trim().ToUpper(), @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$"))
                return "El RFC no tiene un formato válido (12 o 13 caracteres).";

            if (Empleado.FechaNacimiento == null)
                return "Ingresa la fecha de nacimiento.";
            if (Empleado.FechaNacimiento > DateTime.Today.AddYears(-18) || Empleado.FechaNacimiento < DateTime.Today.AddYears(-90))
                return "El empleado debe tener entre 18 y 90 años.";

            if (string.IsNullOrWhiteSpace(Empleado.Correo) || !Regex.IsMatch(Empleado.Correo.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Ingresa un correo válido.";
            if (string.IsNullOrWhiteSpace(Empleado.Telefono) || !Regex.IsMatch(Empleado.Telefono.Trim(), @"^\d{10}$"))
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
