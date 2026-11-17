using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PIA_Gimnasio.Entidades;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Lista de rutinas. El entrenador solo ve las que él diseñó.
    public class RutinaModel : PaginaProtegida
    {
        private readonly logicaRutina _logica;

        public RutinaModel(logicaRutina logica)
        {
            _logica = logica;
            RolesPermitidos = new string[] { "ADMINISTRADOR", "ENTRENADOR" };
        }

        // Filtros de búsqueda
        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdCliente { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BusquedaIdEmpleado { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BusquedaObjetivo { get; set; }

        // Para los combos y la tabla
        public List<SelectListItem> Socios { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Entrenadores { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Objetivos { get; set; } = new List<SelectListItem>();
        public List<DataRow> Rutinas { get; set; } = new List<DataRow>();

        public void OnGet()
        {
            Cargar();
        }

        // Botón Eliminar (baja lógica)
        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                // El entrenador solo puede eliminar las rutinas que él diseñó
                DataRow? rutina = _logica.Obtener(id);
                if (rutina != null && EsEntrenador && Convert.ToInt32(rutina["IdEmpleado"]) != IdEmpleadoSesion())
                {
                    Mensaje = "Solo puedes eliminar las rutinas que tú diseñaste.";
                    TipoMensaje = "warning";
                }
                else
                {
                    bdRespuesta r = _logica.Eliminar(id, IdUsuarioSesion());
                    Mensaje = r.Mensaje;
                    TipoMensaje = r.TipoMensaje;
                }
            }
            catch (Exception)
            {
                Mensaje = "Ocurrió un error al eliminar la rutina.";
                TipoMensaje = "danger";
            }
            Cargar();
            return Page();
        }

        // Llena combos y tabla
        private void Cargar()
        {
            // El entrenador solo ve las rutinas que él diseñó (sin empleado ligado no ve ninguna)
            if (EsEntrenador)
                BusquedaIdEmpleado = IdEmpleadoSesion() ?? -1;

            Objetivos = CargarCombo(logicaCatalogo.Objetivos);
            try
            {
                Socios = CargarCombo(_logica.ConsultarSocios(), "IdCliente", "Socio");
                Entrenadores = CargarCombo(_logica.ConsultarEntrenadores(), "IdEmpleado", "Entrenador");
                Rutinas = Paginar(_logica.Consultar(BusquedaIdCliente, BusquedaIdEmpleado, BusquedaObjetivo));
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }
        }
    }
}
