using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica.Validacion
{
    // Reglas de captura de los catálogos base (Puestos, Especialidades y Tipos de membresía).
    // Regresan la lista de mensajes; si está vacía, los datos son válidos.
    public static class validacionCatalogo
    {
        // Puestos y especialidades tienen la misma forma: Nombre + Descripción
        public static List<string> Validar(string? nombre, string? descripcion)
        {
            List<string> errores = new List<string>();
            Texto(errores, nombre, "el nombre", 50);
            Texto(errores, descripcion, "la descripción", 50);
            return errores;
        }

        public static readonly string[] RestriccionesAforo = { "No aplica", "Sujeto a aforo", "Sin restricción" };

        public static List<string> ValidarMembresia(bdMembresia m)
        {
            List<string> errores = new List<string>();
            Texto(errores, m.Nombre, "el nombre", 50);
            Texto(errores, m.Descripcion, "la descripción", 50);

            if (!m.Precio.HasValue)
                errores.Add("Ingresa el precio mensual.");
            else if (m.Precio <= 0 || m.Precio > 99999999)
                errores.Add("El precio debe ser mayor a 0.");

            // El límite semanal es el que aplica Reservas (Bronce 0, Plata 3, Oro 5)
            if (!m.MaxClasesSemana.HasValue)
                errores.Add("Ingresa el límite de clases por semana (0 si no incluye clases).");
            else if (m.MaxClasesSemana < 0 || m.MaxClasesSemana > 7)
                errores.Add("El límite de clases por semana debe estar entre 0 y 7.");

            if (string.IsNullOrEmpty(m.RestriccionAforo) || !RestriccionesAforo.Contains(m.RestriccionAforo))
                errores.Add("Selecciona la restricción de aforo.");

            if (!m.IdEstatus.HasValue)
                errores.Add("Selecciona el estatus.");

            return errores;
        }

        // Texto obligatorio con largo máximo
        internal static void Texto(List<string> errores, string? valor, string campo, int maximo, bool obligatorio = true)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                if (obligatorio)
                    errores.Add($"Ingresa {campo}.");
            }
            else if (valor.Trim().Length > maximo)
                errores.Add($"{char.ToUpper(campo[0])}{campo[1..]} no puede pasar de {maximo} caracteres.");
        }
    }
}
