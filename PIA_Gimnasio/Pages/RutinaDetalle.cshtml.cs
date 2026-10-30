using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Permisos: Seguridad/Modulos.cs
    public class RutinaDetalleModel : PaginaBase
    {
        private readonly logicaRutina _logica;

        public RutinaDetalleModel(logicaRutina logica)
        {
            _logica = logica;
        }

        // Para la vista
        public DataRow? Rutina { get; set; }
        public List<DataRow> Detalle { get; set; } = new();
        public DataRow? Expediente { get; set; }
        public List<string> Alertas { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            try
            {
                Rutina = _logica.Obtener(id);
                if (Rutina == null)
                {
                    MostrarMensaje("No se encontró la rutina seleccionada.", "warning");
                    return Redirigir("/Rutina");
                }

                Detalle = _logica.ConsultarDetalle(id).AsEnumerable().ToList();
                Expediente = _logica.ObtenerExpediente(Convert.ToInt32(Rutina["IdCliente"]));

                // Verificación médica de los ejercicios asignados
                Alertas = logicaRutina.RevisarRutina(Expediente, Detalle
                    .Select(d => (d["Ejercicio"].ToString()!, d["ContraIndicacion"].ToString())));
            }
            catch (SqlException)
            {
                MostrarErrorConexion();
            }

            return Page();
        }
    }
}
