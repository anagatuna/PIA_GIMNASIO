using System.Data;
using Microsoft.AspNetCore.Mvc;
using PIA_Gimnasio.Logica;

namespace PIA_Gimnasio.Pages
{
    // Detalle de una rutina con su matriz semanal y la verificación médica
    public class RutinaDetalleModel : PaginaProtegida
    {
        private readonly logicaRutina _logica;

        public RutinaDetalleModel(logicaRutina logica)
        {
            _logica = logica;
            RolesPermitidos = new string[] { "ADMINISTRADOR", "ENTRENADOR" };
        }

        // Para la vista
        public DataRow? Rutina { get; set; }
        public List<DataRow> Detalle { get; set; } = new List<DataRow>();
        public DataRow? Expediente { get; set; }
        public List<string> Alertas { get; set; } = new List<string>();
        public int TotalDias { get; set; }

        // guardada = true cuando viene de "Nueva rutina"
        public IActionResult OnGet(int id, bool guardada)
        {
            try
            {
                Rutina = _logica.Obtener(id);

                // El entrenador solo puede ver las rutinas que él diseñó
                if (Rutina != null && EsEntrenador && Convert.ToInt32(Rutina["IdEmpleado"]) != IdEmpleadoSesion())
                    Rutina = null;

                if (Rutina == null)
                    return RedirectToPage("/Rutina");

                // Ejercicios de la rutina y cuántos días distintos ocupa
                List<string> dias = new List<string>();
                foreach (DataRow d in _logica.ConsultarDetalle(id).Rows)
                {
                    Detalle.Add(d);
                    string dia = d["DiaSemana"].ToString() ?? "";
                    if (!dias.Contains(dia))
                        dias.Add(dia);
                }
                TotalDias = dias.Count;

                Expediente = _logica.ObtenerExpediente(Convert.ToInt32(Rutina["IdCliente"]));

                // Verificación médica de los ejercicios asignados
                foreach (DataRow d in Detalle)
                {
                    string? alerta = logicaRutina.RevisarContraindicacion(Expediente, d["Ejercicio"].ToString() ?? "", d["ContraIndicacion"].ToString());
                    if (alerta != null && !Alertas.Contains(alerta))
                        Alertas.Add(alerta);
                }

                if (guardada)
                {
                    Mensaje = "Rutina registrada correctamente.";
                    TipoMensaje = "success";
                }
            }
            catch (Exception)
            {
                Mensaje = "No se pudo conectar con la base de datos.";
                TipoMensaje = "danger";
            }

            return Page();
        }
    }
}
