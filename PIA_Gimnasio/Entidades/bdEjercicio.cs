namespace PIA_Gimnasio.Entidades
{
    // Representa un ejercicio del catálogo (tabla EJERCICIO)
    public class bdEjercicio
    {
        public int IdEjercicio { get; set; }        
        public string? Nombre { get; set; }
        public string? GrupoMuscular { get; set; }
        public string? NivelDificultad { get; set; }
        public string? ContraIndicacion { get; set; }   // Padecimientos o lesiones separados por coma, o "Ninguna"
        public int? IdEstatus { get; set; } = 1;
    }
}
