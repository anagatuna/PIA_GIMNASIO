using System.Text.RegularExpressions;
using PIA_Gimnasio.Entidades;
using static PIA_Gimnasio.Logica.Validacion.validacionCatalogo;

namespace PIA_Gimnasio.Logica.Validacion
{
    // Formatos compartidos (teléfono, correo, CURP, RFC, fechas)
    public static class validacionFormato
    {
        public static bool Telefono(string? t) => t != null && Regex.IsMatch(t.Trim(), @"^\d{10}$");
        public static bool Correo(string? c) => c != null && Regex.IsMatch(c.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        public static bool CodigoPostal(string? cp) => cp != null && Regex.IsMatch(cp.Trim(), @"^\d{5}$");
        public static bool CURP(string? c) => c != null && Regex.IsMatch(c.Trim().ToUpper(), @"^[A-Z]{4}\d{6}[HMX][A-Z]{5}[A-Z0-9]\d$");
        public static bool RFC(string? r) => r != null && Regex.IsMatch(r.Trim().ToUpper(), @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$");

        public static int Edad(DateTime nacimiento)
        {
            int edad = DateTime.Today.Year - nacimiento.Year;
            return nacimiento.Date > DateTime.Today.AddYears(-edad) ? edad - 1 : edad;
        }

        // Nombre y apellidos de una persona
        public static void NombreCompleto(List<string> errores, string? nombre, string? primerApellido, string? segundoApellido)
        {
            Texto(errores, nombre, "el nombre", 50);
            Texto(errores, primerApellido, "el primer apellido", 50);
            Texto(errores, segundoApellido, "el segundo apellido", 50, obligatorio: false);
        }

        public static void Contacto(List<string> errores, string? correo, string? telefono)
        {
            if (string.IsNullOrWhiteSpace(correo))
                errores.Add("Ingresa el correo.");
            else if (correo.Trim().Length > 50 || !Correo(correo))
                errores.Add("Ingresa un correo válido (máximo 50 caracteres).");

            if (string.IsNullOrWhiteSpace(telefono))
                errores.Add("Ingresa el teléfono.");
            else if (!Telefono(telefono))
                errores.Add("El teléfono debe tener 10 dígitos.");
        }
    }

    // Reglas de captura de sucursales
    public static class validacionSucursal
    {
        public static List<string> Validar(bdSucursal s)
        {
            List<string> errores = new List<string>();
            Texto(errores, s.Nombre, "el nombre de la sucursal", 50);

            if (string.IsNullOrWhiteSpace(s.Telefono))
                errores.Add("Ingresa el teléfono de contacto.");
            else if (!validacionFormato.Telefono(s.Telefono))
                errores.Add("El teléfono debe tener 10 dígitos.");

            // Campo crítico: lo usa Control de acceso para bloquear entradas
            if (!s.AforoMaximo.HasValue)
                errores.Add("Ingresa el aforo máximo permitido.");
            else if (s.AforoMaximo < 1 || s.AforoMaximo > 5000)
                errores.Add("El aforo máximo debe estar entre 1 y 5000 personas.");

            if (!s.HoraApertura.HasValue || !s.HoraCierre.HasValue)
                errores.Add("Ingresa el horario de apertura y cierre.");
            else if (s.HoraCierre <= s.HoraApertura)
                errores.Add("La hora de cierre debe ser posterior a la hora de apertura.");

            Texto(errores, s.Calle, "la calle", 50);
            Texto(errores, s.NumeroInt, "el número", 50);
            Texto(errores, s.Colonia, "la colonia", 50);
            Texto(errores, s.Municipio, "la ciudad o municipio", 50);

            if (!s.IdEstado.HasValue)
                errores.Add("Selecciona el estado.");

            if (string.IsNullOrWhiteSpace(s.CodigoPostal))
                errores.Add("Ingresa el código postal.");
            else if (!validacionFormato.CodigoPostal(s.CodigoPostal))
                errores.Add("El código postal debe tener 5 dígitos.");

            if (!s.IdEstatus.HasValue)
                errores.Add("Selecciona el estatus.");

            return errores;
        }
    }

    // Reglas de captura de empleados
    public static class validacionEmpleado
    {
        public static List<string> Validar(bdEmpleado e, bool esEntrenador)
        {
            List<string> errores = new List<string>();
            validacionFormato.NombreCompleto(errores, e.Nombre, e.PrimerApellido, e.SegundoApellido);

            // CURP y RFC son opcionales en la tabla, pero si se capturan deben tener formato válido
            if (!string.IsNullOrWhiteSpace(e.CURP) && !validacionFormato.CURP(e.CURP))
                errores.Add("La CURP no tiene un formato válido (18 caracteres).");
            if (!string.IsNullOrWhiteSpace(e.RFC) && !validacionFormato.RFC(e.RFC))
                errores.Add("El RFC no tiene un formato válido (12 o 13 caracteres).");

            if (!e.FechaNacimiento.HasValue)
                errores.Add("Ingresa la fecha de nacimiento.");
            else if (validacionFormato.Edad(e.FechaNacimiento.Value) < 18 || validacionFormato.Edad(e.FechaNacimiento.Value) > 90)
                errores.Add("El empleado debe tener entre 18 y 90 años.");

            validacionFormato.Contacto(errores, e.Correo, e.Telefono);

            if (!e.IdSucursal.HasValue)
                errores.Add("Selecciona la sucursal.");
            if (!e.IdPuesto.HasValue)
                errores.Add("Selecciona el puesto.");

            if (!e.FechaContratacion.HasValue)
                errores.Add("Ingresa la fecha de contratación.");
            else if (e.FechaContratacion > DateTime.Today)
                errores.Add("La fecha de contratación no puede ser futura.");
            else if (e.FechaNacimiento.HasValue && e.FechaContratacion < e.FechaNacimiento.Value.AddYears(18))
                errores.Add("La fecha de contratación debe ser posterior a sus 18 años.");

            // Especialidades: obligatorias para un entrenador, no aplican a otros puestos
            if (esEntrenador && e.Especialidades.Count == 0)
                errores.Add("Selecciona al menos una especialidad para el entrenador.");

            if (!e.IdEstatus.HasValue)
                errores.Add("Selecciona el estatus.");

            return errores;
        }
    }

    // Reglas de captura de socios y expediente médico
    public static class validacionSocio
    {
        public static readonly string[] TiposSangre = { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };

        public static List<string> Validar(bdSocio s)
        {
            List<string> errores = new List<string>();
            validacionFormato.NombreCompleto(errores, s.Nombre, s.PrimerApellido, s.SegundoApellido);

            if (!s.FechaNacimiento.HasValue)
                errores.Add("Ingresa la fecha de nacimiento.");
            else if (validacionFormato.Edad(s.FechaNacimiento.Value) < 12 || validacionFormato.Edad(s.FechaNacimiento.Value) > 100)
                errores.Add("El socio debe tener entre 12 y 100 años.");

            validacionFormato.Contacto(errores, s.Correo, s.Telefono);

            if (!s.IdSucursal.HasValue)
                errores.Add("Selecciona la sucursal de inscripción.");
            if (!s.IdEstatus.HasValue)
                errores.Add("Selecciona el estatus.");

            return errores;
        }

        // Expediente médico: el tipo de sangre es obligatorio; lo demás es opcional (máximo 50 caracteres)
        public static List<string> ValidarExpediente(bdExpediente e)
        {
            List<string> errores = new List<string>();

            if (string.IsNullOrEmpty(e.TipoSangre))
                errores.Add("Selecciona el tipo de sangre (el expediente médico es obligatorio).");
            else if (!TiposSangre.Contains(e.TipoSangre))
                errores.Add("Selecciona un tipo de sangre de la lista.");

            Texto(errores, e.PadecimientosCronicos, "los padecimientos crónicos", 50, obligatorio: false);
            Texto(errores, e.LesionesPrevias, "las lesiones previas", 50, obligatorio: false);
            Texto(errores, e.Alergias, "las alergias", 50, obligatorio: false);
            Texto(errores, e.CondicionRiesgo, "la condición de riesgo", 50, obligatorio: false);

            return errores;
        }
    }

    // Reglas de captura de operación diaria (suscripciones, clases y reservas)
    public static class validacionOperacion
    {
        public static List<string> ValidarSuscripcion(bdSuscripcion s)
        {
            List<string> errores = new List<string>();

            if (!s.IdCliente.HasValue)
                errores.Add("Selecciona el socio.");
            if (!s.IdTipoMembresia.HasValue)
                errores.Add("Selecciona la membresía.");

            if (!s.FechaInicio.HasValue)
                errores.Add("Ingresa la fecha de inicio.");
            else if (s.FechaInicio.Value.Date < DateTime.Today)
                errores.Add("La fecha de inicio no puede ser anterior a hoy.");
            else if (s.FechaInicio.Value.Date > DateTime.Today.AddMonths(1))
                errores.Add("La fecha de inicio no puede ser mayor a un mes a partir de hoy.");

            return errores;
        }

        public static List<string> ValidarClase(bdClase c)
        {
            List<string> errores = new List<string>();
            Texto(errores, c.Nombre, "el nombre de la clase", 50);
            Texto(errores, c.Salon, "el salón", 50);

            if (!c.Fecha.HasValue)
                errores.Add("Ingresa la fecha de la clase.");
            else
            {
                if (c.Fecha.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    errores.Add("Las clases grupales solo se programan de lunes a viernes.");
                if (c.Fecha.Value.Date < DateTime.Today)
                    errores.Add("No se pueden programar clases en fechas pasadas.");
            }

            if (!c.HoraInicio.HasValue || !c.HoraFin.HasValue)
                errores.Add("Ingresa la hora de inicio y de fin.");
            else if (c.HoraFin <= c.HoraInicio)
                errores.Add("La hora de fin debe ser posterior a la hora de inicio.");

            if (!c.CupoMaximo.HasValue)
                errores.Add("Ingresa el cupo máximo del salón.");
            else if (c.CupoMaximo < 1 || c.CupoMaximo > 500)
                errores.Add("El cupo máximo debe estar entre 1 y 500.");

            if (!c.IdSucursal.HasValue)
                errores.Add("Selecciona la sucursal.");
            if (!c.IdEmpleado.HasValue)
                errores.Add("Selecciona el entrenador.");

            return errores;
        }

        public static List<string> ValidarReserva(bdReserva r)
        {
            List<string> errores = new List<string>();
            if (!r.IdCliente.HasValue)
                errores.Add("Selecciona el socio.");
            if (!r.IdClase.HasValue)
                errores.Add("Selecciona la clase.");
            return errores;
        }
    }
}
