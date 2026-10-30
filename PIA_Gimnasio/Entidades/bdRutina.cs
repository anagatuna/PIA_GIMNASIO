namespace PIA_Gimnasio.Entidades
{
    // Cabecera de una rutina (tabla RUTINA) con sus ejercicios (DETALLE_RUTINA)
    public class bdRutina
    {
        public int IdRutina { get; set; }
        public int? IdCliente { get; set; }         // Socio asignado
        public int? IdEmpleado { get; set; }        // Entrenador que la diseña
        public string? Nombre { get; set; }
        public string? Objetivo { get; set; }
        public DateTime? FechaAsignacion { get; set; } = DateTime.Today;
        public List<bdDetalleRutina> Detalles { get; set; } = new();   // Matriz semanal
    }
}
