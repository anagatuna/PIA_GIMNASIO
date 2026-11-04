namespace PIA_Gimnasio.Entidades
{
    // Sede física de la cadena (tabla SUCURSAL)
    public class bdSucursal
    {
        public int IdSucursal { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public int? AforoMaximo { get; set; }           // Campo crítico: bloquea entradas en Control de acceso
        public TimeSpan? HoraApertura { get; set; }
        public TimeSpan? HoraCierre { get; set; }
        public string? Calle { get; set; }
        public string? NumeroInt { get; set; }          // Número exterior / interior
        public string? Colonia { get; set; }
        public string? Municipio { get; set; }
        public int? IdEstado { get; set; }
        public string? CodigoPostal { get; set; }
        public int? IdEstatus { get; set; } = 1;
    }
}
