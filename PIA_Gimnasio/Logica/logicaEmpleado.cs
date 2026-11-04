using System.Data;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Empleados y sus especialidades (spEmpleado)
    public class logicaEmpleado : logicaBase
    {
        protected override string Procedimiento => "spEmpleado";
        public logicaEmpleado(bdConexion bd) : base(bd) { }

        // Las especialidades viajan como "1,3,5"; el SP las ignora si el puesto no es Entrenador
        public bdRespuesta Guardar(bdEmpleado e, int usuarioSesion) =>
            Respuesta(e.IdEmpleado == 0 ? 1 : 4,
                ("@IdEmpleado", e.IdEmpleado), ("@IdSucursal", Valor(e.IdSucursal)), ("@IdPuesto", Valor(e.IdPuesto)),
                ("@IdEstatus", Valor(e.IdEstatus)), ("@Nombre", Mayusculas(e.Nombre)), ("@PrimerApellido", Mayusculas(e.PrimerApellido)),
                ("@SegundoApellido", Mayusculas(e.SegundoApellido)), ("@CURP", Mayusculas(e.CURP)), ("@RFC", Mayusculas(e.RFC)),
                ("@FechaNacimiento", Valor(e.FechaNacimiento)), ("@Correo", Valor(e.Correo?.ToLower())), ("@Telefono", Valor(e.Telefono)),
                ("@FechaContratacion", Valor(e.FechaContratacion)), ("@Especialidades", string.Join(",", e.Especialidades)),
                ("@UsuarioSesion", usuarioSesion));

        public bdEmpleado? Obtener(int id)
        {
            DataRow? r = Renglon(2, ("@IdEmpleado", id));
            return r == null ? null : new bdEmpleado
            {
                IdEmpleado = Convert.ToInt32(r["IdEmpleado"]),
                IdSucursal = Entero(r, "IdSucursal"),
                IdPuesto = Entero(r, "IdPuesto"),
                IdEstatus = Entero(r, "IdEstatus"),
                Nombre = Texto(r, "Nombre"),
                PrimerApellido = Texto(r, "PrimerApellido"),
                SegundoApellido = Texto(r, "SegundoApellido"),
                CURP = Texto(r, "CURP"),
                RFC = Texto(r, "RFC"),
                FechaNacimiento = Fecha(r, "FechaNacimiento"),
                Correo = Texto(r, "Correo"),
                Telefono = Texto(r, "Telefono"),
                FechaContratacion = Fecha(r, "FechaContratacion"),
                Especialidades = (Texto(r, "Especialidades") ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
            };
        }

        public DataTable Consultar(string? nombre, int? idSucursal, int? idPuesto, int? idEstatus) =>
            Ejecutar(3, ("@Nombre", Valor(nombre)), ("@IdSucursal", Valor(idSucursal)),
                ("@IdPuesto", Valor(idPuesto)), ("@IdEstatus", Valor(idEstatus)));

        public bdRespuesta Eliminar(int id, int usuarioSesion) =>
            Respuesta(5, ("@IdEmpleado", id), ("@UsuarioSesion", usuarioSesion));

        public DataTable ConsultarPuestos() => Ejecutar(6);
        public DataTable ConsultarEspecialidades() => Ejecutar(7);

        // Entrenadores activos (opcionalmente de una sucursal) para programar clases
        public DataTable ConsultarEntrenadores(int? idSucursal = null) => Ejecutar(8, ("@IdSucursal", Valor(idSucursal)));
    }
}
