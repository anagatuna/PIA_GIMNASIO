namespace PIA_Gimnasio.Entidades
{
    // Lo que regresan los SP al registrar, modificar o eliminar
    public class bdRespuesta
    {
        public string Mensaje { get; set; } = "";
        public string TipoMensaje { get; set; } = "";
        public int Id { get; set; }     // Id del registro nuevo, cuando el SP lo regresa (ej. IdRutina)
    }
}
