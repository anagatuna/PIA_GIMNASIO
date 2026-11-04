namespace PIA_Gimnasio.Entidades
{
    // Plan comercial (tabla TIPO_MEMBRESIA): Bronce, Plata, Oro...
    public class bdMembresia
    {
        public int IdTipoMembresia { get; set; }
        public string? Nombre { get; set; }
        public decimal? Precio { get; set; }            // Precio mensual
        public string? Descripcion { get; set; }
        public int? MaxClasesSemana { get; set; }       // Límite de clases grupales por semana (Bronce 0, Plata 3, Oro 5)
        public string? RestriccionAforo { get; set; }
        public int? IdEstatus { get; set; } = 1;
    }
}
