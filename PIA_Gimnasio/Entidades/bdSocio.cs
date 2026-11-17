namespace PIA_Gimnasio.Entidades
{
    public class bdSocio
    {
        public int IdCliente { get; set; }
        public int? Folio { get; set; }                 // Lo genera desde el SP: Año actual + num consecutivo 
        public string? Nombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public int? IdSucursal { get; set; }
        public int? IdEstatus { get; set; } = 1;
        public DateTime? FechaInscripcion { get; set; }
    }
}
