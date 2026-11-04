namespace PIA_Gimnasio.Entidades
{
    // Personal del gimnasio (tabla EMPLEADO + EMPLEADO_ESPECIALIDAD)
    public class bdEmpleado
    {
        public int IdEmpleado { get; set; }
        public string? Nombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? CURP { get; set; }
        public string? RFC { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public int? IdSucursal { get; set; }
        public int? IdPuesto { get; set; }
        public DateTime? FechaContratacion { get; set; } = DateTime.Today;
        public int? IdEstatus { get; set; } = 1;
        public List<int> Especialidades { get; set; } = new();   // Solo si el puesto es Entrenador
    }
}
