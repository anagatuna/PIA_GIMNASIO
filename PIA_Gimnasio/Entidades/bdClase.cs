namespace PIA_Gimnasio.Entidades
{
    // Clase grupal programada (tabla CLASE), de lunes a viernes
    public class bdClase
    {
        public int IdClase { get; set; }
        public string? Nombre { get; set; }
        public DateTime? Fecha { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public string? Salon { get; set; }
        public int? CupoMaximo { get; set; }
        public int? IdSucursal { get; set; }
        public int? IdEmpleado { get; set; }            // Entrenador asignado
    }
}
