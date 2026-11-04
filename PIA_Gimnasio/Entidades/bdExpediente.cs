namespace PIA_Gimnasio.Entidades
{
    // Ficha clínica del socio (tabla EXPEDIENTE_MEDICO). Obligatoria para asignar rutinas.
    public class bdExpediente
    {
        public int IdCliente { get; set; }
        public string? TipoSangre { get; set; }
        public string? PadecimientosCronicos { get; set; }
        public string? LesionesPrevias { get; set; }
        public string? Alergias { get; set; }
        public string? CondicionRiesgo { get; set; }
    }
}
