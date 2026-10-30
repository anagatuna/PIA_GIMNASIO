namespace PIA_Gimnasio.Entidades
{
    // Un ejercicio dentro de la matriz semanal de una rutina (tabla DETALLE_RUTINA)
    public class bdDetalleRutina
    {
        public int IdEjercicio { get; set; }
        public string? DiaSemana { get; set; }
        public int Series { get; set; }
        public int Repeticiones { get; set; }
        public int TiempoDescanso { get; set; }    // En segundos
    }
}
