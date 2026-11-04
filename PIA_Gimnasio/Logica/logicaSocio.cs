using System.Data;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Socios (spSocio). El registro incluye el expediente médico obligatorio.
    public class logicaSocio : logicaBase
    {
        protected override string Procedimiento => "spSocio";
        public logicaSocio(bdConexion bd) : base(bd) { }

        // Accion 1 (registrar socio + expediente; regresa el folio generado) o 4 (modificar datos del socio)
        public (bdRespuesta Respuesta, int Folio) Guardar(bdSocio s, bdExpediente expediente, int usuarioSesion)
        {
            DataTable dt = Ejecutar(s.IdCliente == 0 ? 1 : 4,
                ("@IdCliente", s.IdCliente), ("@IdSucursal", Valor(s.IdSucursal)), ("@IdEstatus", Valor(s.IdEstatus)),
                ("@Nombre", Mayusculas(s.Nombre)), ("@PrimerApellido", Mayusculas(s.PrimerApellido)),
                ("@SegundoApellido", Mayusculas(s.SegundoApellido)), ("@FechaNacimiento", Valor(s.FechaNacimiento)),
                ("@Correo", Valor(s.Correo?.ToLower())), ("@Telefono", Valor(s.Telefono)),
                ("@TipoSangre", Valor(expediente.TipoSangre)), ("@PadecimientosCronicos", Valor(expediente.PadecimientosCronicos)),
                ("@LesionesPrevias", Valor(expediente.LesionesPrevias)), ("@Alergias", Valor(expediente.Alergias)),
                ("@CondicionRiesgo", Valor(expediente.CondicionRiesgo)), ("@UsuarioSesion", usuarioSesion));

            return (Respuesta(dt), Convert.ToInt32(dt.Rows[0]["Folio"]));
        }

        public bdSocio? Obtener(int id)
        {
            DataRow? r = Renglon(2, ("@IdCliente", id));
            return r == null ? null : new bdSocio
            {
                IdCliente = Convert.ToInt32(r["IdCliente"]),
                Folio = Entero(r, "Folio"),
                IdSucursal = Entero(r, "IdSucursal"),
                IdEstatus = Entero(r, "IdEstatus"),
                Nombre = Texto(r, "Nombre"),
                PrimerApellido = Texto(r, "PrimerApellido"),
                SegundoApellido = Texto(r, "SegundoApellido"),
                FechaNacimiento = Fecha(r, "FechaNacimiento"),
                Correo = Texto(r, "Correo"),
                Telefono = Texto(r, "Telefono"),
                FechaInscripcion = Fecha(r, "FechaInscripcion")
            };
        }

        // Búsqueda por folio o nombre
        public DataTable Consultar(string? busqueda, int? idSucursal, int? idEstatus) =>
            Ejecutar(3, ("@Nombre", Valor(busqueda)), ("@IdSucursal", Valor(idSucursal)), ("@IdEstatus", Valor(idEstatus)));

        public bdRespuesta Eliminar(int id, int usuarioSesion) =>
            Respuesta(5, ("@IdCliente", id), ("@UsuarioSesion", usuarioSesion));

        // Socios activos (combos de suscripciones y reservas)
        public DataTable ConsultarActivos() => Ejecutar(6);
    }

    // Expediente médico (spExpedienteMedico)
    public class logicaExpediente : logicaBase
    {
        protected override string Procedimiento => "spExpedienteMedico";
        public logicaExpediente(bdConexion bd) : base(bd) { }

        public bdRespuesta Guardar(bdExpediente e, int usuarioSesion) =>
            Respuesta(1, ("@IdCliente", e.IdCliente), ("@TipoSangre", Valor(e.TipoSangre)),
                ("@PadecimientosCronicos", Valor(e.PadecimientosCronicos)), ("@LesionesPrevias", Valor(e.LesionesPrevias)),
                ("@Alergias", Valor(e.Alergias)), ("@CondicionRiesgo", Valor(e.CondicionRiesgo)), ("@UsuarioSesion", usuarioSesion));

        // Datos del socio + expediente (las columnas del expediente vienen en NULL si no tiene)
        public DataRow? Obtener(int idCliente) => Renglon(2, ("@IdCliente", idCliente));

        public DataTable Consultar(string? busqueda) => Ejecutar(3, ("@Busqueda", Valor(busqueda)));
    }
}
