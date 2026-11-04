namespace PIA_Gimnasio.Entidades
{
    // Venta de una membresía a un socio (tabla SUSCRIPCION). La vigencia de 1 mes la calcula el SP.
    public class bdSuscripcion
    {
        public int? IdCliente { get; set; }
        public int? IdTipoMembresia { get; set; }
        public DateTime? FechaInicio { get; set; } = DateTime.Today;

        // Mismo cálculo que spSuscripcion: un mes menos un día (del 4 de nov al 3 de dic)
        public static DateTime CalcularVencimiento(DateTime inicio) => inicio.AddMonths(1).AddDays(-1);
    }
}
