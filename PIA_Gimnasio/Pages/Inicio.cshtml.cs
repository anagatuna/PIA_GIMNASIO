namespace PIA_Gimnasio.Pages
{
    // Pantalla de inicio: bienvenida al usuario según su rol
    public class InicioModel : PaginaProtegida
    {
        public string Nombre { get; set; } = "";
        public string Rol { get; set; } = "";
        public string DescripcionRol { get; set; } = "";

        public void OnGet()
        {
            Nombre = HttpContext.Session.GetString("Nombre") ?? "";
            Rol = RolSesion();

            if (Rol == "Administrador")
                DescripcionRol = "Tienes acceso total al sistema, incluida la creación de cuentas de usuario.";
            else if (Rol == "Recepcionista")
                DescripcionRol = "Registra socios, controla el acceso de tu sucursal y captura reservas de clases.";
            else if (Rol == "Entrenador")
                DescripcionRol = "Consulta expedientes médicos, diseña y asigna rutinas y pasa lista en tus clases.";
        }
    }
}
