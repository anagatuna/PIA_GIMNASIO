using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica.Validacion
{
    // Reglas de captura de rutinas. Regresa la lista de mensajes; si está vacía, los datos son válidos.
    // (El expediente médico y las contraindicaciones se revisan contra la BD con logicaRutina.)
    public static class validacionRutina
    {
        // Valores permitidos (la tabla ya no tiene CHECK; también se validan en spRutina)
        public static readonly string[] DiasSemana = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        public static readonly string[] Objetivos = { "Hipertrofia", "Pérdida de peso", "Reacondicionamiento", "Fuerza", "Resistencia" };

        public static List<string> Validar(bdRutina rutina, HashSet<int> ejerciciosActivos)
        {
            List<string> errores = new List<string>();

            // Cabecera
            if (!rutina.IdCliente.HasValue)
                errores.Add("Selecciona el socio.");
            if (!rutina.IdEmpleado.HasValue)
                errores.Add("Selecciona el entrenador.");
            if (string.IsNullOrWhiteSpace(rutina.Nombre))
                errores.Add("Ingresa el nombre de la rutina.");
            else if (rutina.Nombre.Trim().Length > 50)
                errores.Add("El nombre no puede pasar de 50 caracteres.");
            if (string.IsNullOrEmpty(rutina.Objetivo))
                errores.Add("Selecciona el objetivo.");
            else if (!Objetivos.Contains(rutina.Objetivo))
                errores.Add("Selecciona un objetivo de la lista.");
            if (!rutina.FechaAsignacion.HasValue)
                errores.Add("Selecciona la fecha de asignación.");

            // Matriz semanal
            if (rutina.Detalles.Count == 0)
                errores.Add("Agrega al menos un ejercicio a la rutina.");

            foreach (bdDetalleRutina d in rutina.Detalles)
            {
                if (!DiasSemana.Contains(d.DiaSemana))
                    errores.Add($"\"{d.DiaSemana}\" no es un día válido. Usa de Lunes a Domingo.");
                if (d.Series <= 0)
                    errores.Add("Las series deben ser mayores a 0.");
                if (d.Repeticiones <= 0)
                    errores.Add("Las repeticiones deben ser mayores a 0.");
                if (d.TiempoDescanso < 0)
                    errores.Add("El tiempo de descanso no puede ser negativo.");
                if (!ejerciciosActivos.Contains(d.IdEjercicio))
                    errores.Add("Hay ejercicios que no existen o están inactivos.");
            }

            return errores.Distinct().ToList();
        }
    }
}
