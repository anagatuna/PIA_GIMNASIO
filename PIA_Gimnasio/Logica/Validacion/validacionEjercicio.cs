using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica.Validacion
{
    // Reglas de captura de ejercicios. Regresa la lista de mensajes; si está vacía, los datos son válidos.
    public static class validacionEjercicio
    {
        // Valores permitidos (la tabla ya no tiene CHECK; también se validan en spEjercicio)
        public static readonly string[] NivelesDificultad = { "Principiante", "Intermedio", "Avanzado" };
        public static readonly string[] GruposMusculares = { "Pecho", "Espalda", "Pierna", "Glúteo", "Hombro", "Brazo", "Abdomen", "Cardio", "Cuerpo completo" };

        public static List<string> Validar(bdEjercicio ejercicio)
        {
            List<string> errores = new List<string>();

            if (string.IsNullOrWhiteSpace(ejercicio.Nombre))
                errores.Add("Ingresa el nombre del ejercicio.");
            else if (ejercicio.Nombre.Trim().Length > 50)
                errores.Add("El nombre no puede pasar de 50 caracteres.");

            if (string.IsNullOrEmpty(ejercicio.GrupoMuscular))
                errores.Add("Selecciona el grupo muscular.");
            else if (!GruposMusculares.Contains(ejercicio.GrupoMuscular))
                errores.Add("Selecciona un grupo muscular de la lista.");

            // Nivel de dificultad: estrictamente uno de los valores permitidos
            if (string.IsNullOrEmpty(ejercicio.NivelDificultad))
                errores.Add("Selecciona el nivel de dificultad.");
            else if (!NivelesDificultad.Contains(ejercicio.NivelDificultad))
                errores.Add("El nivel de dificultad debe ser Principiante, Intermedio o Avanzado.");

            if (string.IsNullOrWhiteSpace(ejercicio.ContraIndicacion))
                errores.Add("Ingresa la contraindicación (o \"Ninguna\").");
            else if (ejercicio.ContraIndicacion.Trim().Length > 50)
                errores.Add("La contraindicación no puede pasar de 50 caracteres.");

            if (!ejercicio.IdEstatus.HasValue)
                errores.Add("Selecciona el estatus.");

            return errores;
        }
    }
}
