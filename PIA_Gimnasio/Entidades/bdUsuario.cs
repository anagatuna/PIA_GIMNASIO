namespace PIA_Gimnasio.Entidades
{
    // Representa un usuario del sistema (tabla USUARIO)
    public class bdUsuario
    {
        public int IdUsuario { get; set; }          
        public string? Cuenta { get; set; }
        public string? Contrasenia { get; set; }   
        public int? IdEmpleado { get; set; }        // Empleado al que pertenece la cuenta (relación 1:1)
        public int? IdRol { get; set; }
        public int? IdEstatus { get; set; } = 1;
    }
}
