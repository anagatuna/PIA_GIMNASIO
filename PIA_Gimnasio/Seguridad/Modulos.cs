using System.Security.Claims;

namespace PIA_Gimnasio.Seguridad
{
    // Una pantalla del sistema y los roles que pueden entrar
    public record Modulo(string Seccion, string Titulo, string Pagina, string Icono, string Descripcion, params string[] Roles);

    // Catálogo único de permisos (matriz RBAC del documento de arquitectura).
    // Lo usan el menú, el panel de inicio y la política "PorModulo" que protege todas las páginas.
    public static class Modulos
    {
        private const string A = Seguridad.Roles.Administrador;
        private const string R = Seguridad.Roles.Recepcionista;
        private const string E = Seguridad.Roles.Entrenador;

        // Páginas para cualquier usuario con sesión
        private static readonly string[] Generales = { "/Inicio", "/Perfil", "/AccesoDenegado", "/Privacidad", "/Error", "/Login" };

        public static readonly Modulo[] Todos =
        {
            // Socios y suscripciones
            new("Socios", "Socios", "/Socio", "bi-people", "Registro y consulta de socios con folio único.", A, R),
            new("Socios", "Expediente médico", "/ExpedienteMedico", "bi-heart-pulse", "Ficha clínica del socio (solo lectura para entrenadores).", A, R, E),
            new("Socios", "Suscripciones", "/Suscripcion", "bi-credit-card-2-front", "Venta y renovación de membresías.", A, R),

            // Operación diaria
            new("Operación", "Control de acceso", "/ControlAcceso", "bi-door-open", "Entradas, salidas y aforo de la sucursal.", A, R),
            new("Operación", "Clases grupales", "/Clase", "bi-calendar3", "Programación de clases de lunes a viernes.", A),
            new("Operación", "Reservas", "/Reserva", "bi-bookmark-check", "Captura de reservas de los socios.", A, R),
            new("Operación", "Pase de lista", "/PaseLista", "bi-list-check", "Clases del día y registro de asistencia.", A, E),

            // Entrenamiento
            new("Entrenamiento", "Catálogo de ejercicios", "/Ejercicio", "bi-activity", "Ejercicios por grupo muscular, nivel y contraindicación.", A, E),
            new("Entrenamiento", "Rutinas", "/Rutina", "bi-clipboard2-pulse", "Diseño y asignación semanal de rutinas.", A, E),

            // Administración
            new("Administración", "Sucursales", "/Sucursal", "bi-building", "Sedes, horario y aforo máximo.", A),
            new("Administración", "Empleados", "/Empleado", "bi-person-workspace", "Personal, puesto y especialidades.", A),
            new("Administración", "Puestos", "/Puesto", "bi-briefcase", "Catálogo de puestos.", A),
            new("Administración", "Especialidades", "/Especialidad", "bi-award", "Disciplinas de los entrenadores.", A),
            new("Administración", "Tipos de membresía", "/Membresia", "bi-tags", "Planes Bronce, Plata y Oro.", A),
            new("Administración", "Usuarios", "/Usuario", "bi-shield-lock", "Cuentas de acceso de los empleados.", A),

            // Reportes
            new("Reportes", "Empleados por sucursal", "/ReporteEmpleados", "bi-file-earmark-person", "Distribución del personal.", A),
            new("Reportes", "Socios y membresías", "/ReporteSocios", "bi-file-earmark-text", "Estado de las suscripciones.", A, R),
            new("Reportes", "Control de acceso", "/ReporteControlAcceso", "bi-file-earmark-bar-graph", "Entradas, salidas y permanencia.", A, R),
            new("Reportes", "Clases y asistencia", "/ReporteClases", "bi-file-earmark-check", "Demanda y ocupación de clases.", A, E),
            new("Reportes", "Rutinas asignadas", "/ReporteRutinas", "bi-file-earmark-medical", "Rutinas y ejercicios por socio.", A, E),
        };

        // Módulos que puede ver el usuario, en el orden del catálogo
        public static IEnumerable<Modulo> PermitidosPara(ClaimsPrincipal usuario)
        {
            return Todos.Where(m => m.Roles.Any(usuario.IsInRole));
        }

        // Módulo al que pertenece una página: el del catálogo con el nombre más largo que coincide al inicio.
        // Así "/RutinaNueva" y "/RutinaDetalle" pertenecen a "/Rutina", y "/SocioEditar" pertenecería a "/Socio".
        public static Modulo? ModuloDe(string? pagina)
        {
            if (string.IsNullOrEmpty(pagina))
                return null;

            return Todos
                .Where(m => pagina.StartsWith(m.Pagina, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.Pagina.Length)
                .FirstOrDefault();
        }

        // ¿El usuario puede entrar a esta página?
        //  1. Las páginas generales (Inicio, Perfil, Privacidad...) son para cualquier usuario con sesión.
        //  2. Las demás usan los roles de su módulo (ver ModuloDe).
        //  3. Una página que no pertenece a ningún módulo queda solo para el Administrador.
        public static bool PuedeEntrar(ClaimsPrincipal usuario, string? pagina)
        {
            if (string.IsNullOrEmpty(pagina) || Generales.Contains(pagina, StringComparer.OrdinalIgnoreCase))
                return true;

            Modulo? modulo = ModuloDe(pagina);
            return modulo != null
                ? modulo.Roles.Any(usuario.IsInRole)
                : usuario.IsInRole(Seguridad.Roles.Administrador);
        }
    }
}
