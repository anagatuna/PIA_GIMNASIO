namespace PIA_Gimnasio.Entidades
{
    public class bdRespuesta
    {
        public string Mensaje { get; set; } = "";
        public string TipoMensaje { get; set; } = "";
        public int Id { get; set; }     // Id del registro nuevo, cuando el SP lo regresa (ej. IdRutina)
    }
}
