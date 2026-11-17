namespace PIA_Gimnasio.Entidades
{
    public class bdSuscripcion
    {
        public int? IdCliente { get; set; }
        public int? IdTipoMembresia { get; set; }
        public DateTime? FechaInicio { get; set; } = DateTime.Today;

        // Mismo cálculo que spSuscripcion: un mes menos un día
        public static DateTime CalcularVencimiento(DateTime inicio)
        {
            return inicio.AddMonths(1).AddDays(-1);
        }
    }
}
