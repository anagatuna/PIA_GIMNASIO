namespace PIA_Gimnasio.Entidades
{
    // Lo que regresan los SP al registrar, modificar o eliminar
    public class bdRespuesta
    {
        public string Mensaje { get; set; } = "";
        public string TipoMensaje { get; set; } = "";
    }
}
