using System.Data;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Sucursales (spSucursal)
    public class logicaSucursal : logicaBase
    {
        protected override string Procedimiento => "spSucursal";
        public logicaSucursal(bdConexion bd) : base(bd) { }

        public bdRespuesta Guardar(bdSucursal s, int usuarioSesion) =>
            Respuesta(s.IdSucursal == 0 ? 1 : 4,
                ("@IdSucursal", s.IdSucursal), ("@IdEstatus", Valor(s.IdEstatus)), ("@IdEstado", Valor(s.IdEstado)),
                ("@Nombre", Mayusculas(s.Nombre)), ("@Telefono", Valor(s.Telefono)), ("@AforoMaximo", Valor(s.AforoMaximo)),
                ("@HoraApertura", Valor(s.HoraApertura)), ("@HoraCierre", Valor(s.HoraCierre)),
                ("@Calle", Valor(s.Calle)), ("@NumeroInt", Valor(s.NumeroInt)), ("@Colonia", Valor(s.Colonia)),
                ("@Municipio", Valor(s.Municipio)), ("@CodigoPostal", Valor(s.CodigoPostal)), ("@UsuarioSesion", usuarioSesion));

        public bdSucursal? Obtener(int id)
        {
            DataRow? r = Renglon(2, ("@IdSucursal", id));
            return r == null ? null : new bdSucursal
            {
                IdSucursal = Convert.ToInt32(r["IdSucursal"]),
                IdEstatus = Entero(r, "IdEstatus"),
                IdEstado = Entero(r, "IdEstado"),
                Nombre = Texto(r, "Nombre"),
                Telefono = Texto(r, "Telefono"),
                AforoMaximo = Entero(r, "AforoMaximo"),
                HoraApertura = Hora(r, "HoraApertura"),
                HoraCierre = Hora(r, "HoraCierre"),
                Calle = Texto(r, "Calle"),
                NumeroInt = Texto(r, "NumeroInt"),
                Colonia = Texto(r, "Colonia"),
                Municipio = Texto(r, "Municipio"),
                CodigoPostal = Texto(r, "CodigoPostal")
            };
        }

        public DataTable Consultar(string? nombre, int? idEstado, int? idEstatus) =>
            Ejecutar(3, ("@Nombre", Valor(nombre)), ("@IdEstado", Valor(idEstado)), ("@IdEstatus", Valor(idEstatus)));

        public bdRespuesta Eliminar(int id, int usuarioSesion) =>
            Respuesta(5, ("@IdSucursal", id), ("@UsuarioSesion", usuarioSesion));

        // Sucursales activas (combos de empleados, socios, clases y control de acceso)
        public DataTable ConsultarActivas() => Ejecutar(6);

        public DataTable ConsultarEstados() => Ejecutar(7);
    }
}
