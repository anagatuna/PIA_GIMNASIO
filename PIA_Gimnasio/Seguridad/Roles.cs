namespace PIA_Gimnasio.Seguridad
{
    // Nombres de los roles tal como están en la tabla ROL (se guardan en el claim de rol al iniciar sesión)
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Recepcionista = "Recepcionista";
        public const string Entrenador = "Entrenador";
    }
}
