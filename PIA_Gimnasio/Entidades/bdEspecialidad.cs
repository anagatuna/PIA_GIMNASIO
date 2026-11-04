namespace PIA_Gimnasio.Entidades
{
    // Disciplina que puede impartir un entrenador (tabla ESPECIALIDAD)
    public class bdEspecialidad
    {
        public int IdEspecialidad { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
    }
}
