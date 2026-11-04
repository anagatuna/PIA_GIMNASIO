using System.Data;
using PIA_Gimnasio.Datos;
using PIA_Gimnasio.Entidades;

namespace PIA_Gimnasio.Logica
{
    // Catálogo de puestos (spPuesto)
    public class logicaPuesto : logicaBase
    {
        protected override string Procedimiento => "spPuesto";
        public logicaPuesto(bdConexion bd) : base(bd) { }

        // Accion 1 (registrar) o 4 (modificar)
        public bdRespuesta Guardar(bdPuesto puesto, int usuarioSesion) =>
            Respuesta(puesto.IdPuesto == 0 ? 1 : 4,
                ("@IdPuesto", puesto.IdPuesto), ("@Nombre", Valor(puesto.Nombre)),
                ("@Descripcion", Valor(puesto.Descripcion)), ("@UsuarioSesion", usuarioSesion));

        public bdPuesto? Obtener(int id)
        {
            DataRow? r = Renglon(2, ("@IdPuesto", id));
            return r == null ? null : new bdPuesto
            {
                IdPuesto = Convert.ToInt32(r["IdPuesto"]),
                Nombre = Texto(r, "Nombre"),
                Descripcion = Texto(r, "Descripcion")
            };
        }

        public DataTable Consultar(string? nombre) => Ejecutar(3, ("@Nombre", Valor(nombre)));

        public bdRespuesta Eliminar(int id, int usuarioSesion) =>
            Respuesta(5, ("@IdPuesto", id), ("@UsuarioSesion", usuarioSesion));
    }

    // Catálogo de especialidades (spEspecialidad)
    public class logicaEspecialidad : logicaBase
    {
        protected override string Procedimiento => "spEspecialidad";
        public logicaEspecialidad(bdConexion bd) : base(bd) { }

        public bdRespuesta Guardar(bdEspecialidad especialidad, int usuarioSesion) =>
            Respuesta(especialidad.IdEspecialidad == 0 ? 1 : 4,
                ("@IdEspecialidad", especialidad.IdEspecialidad), ("@Nombre", Valor(especialidad.Nombre)),
                ("@Descripcion", Valor(especialidad.Descripcion)), ("@UsuarioSesion", usuarioSesion));

        public bdEspecialidad? Obtener(int id)
        {
            DataRow? r = Renglon(2, ("@IdEspecialidad", id));
            return r == null ? null : new bdEspecialidad
            {
                IdEspecialidad = Convert.ToInt32(r["IdEspecialidad"]),
                Nombre = Texto(r, "Nombre"),
                Descripcion = Texto(r, "Descripcion")
            };
        }

        public DataTable Consultar(string? nombre) => Ejecutar(3, ("@Nombre", Valor(nombre)));

        public bdRespuesta Eliminar(int id, int usuarioSesion) =>
            Respuesta(5, ("@IdEspecialidad", id), ("@UsuarioSesion", usuarioSesion));
    }

    // Tipos de membresía (spMembresia)
    public class logicaMembresia : logicaBase
    {
        protected override string Procedimiento => "spMembresia";
        public logicaMembresia(bdConexion bd) : base(bd) { }

        public bdRespuesta Guardar(bdMembresia m, int usuarioSesion) =>
            Respuesta(m.IdTipoMembresia == 0 ? 1 : 4,
                ("@IdTipoMembresia", m.IdTipoMembresia), ("@IdEstatus", Valor(m.IdEstatus)),
                ("@Nombre", Valor(m.Nombre)), ("@Precio", Valor(m.Precio)), ("@Descripcion", Valor(m.Descripcion)),
                ("@MaxClasesSemana", Valor(m.MaxClasesSemana)), ("@RestriccionAforo", Valor(m.RestriccionAforo)),
                ("@UsuarioSesion", usuarioSesion));

        public bdMembresia? Obtener(int id)
        {
            DataRow? r = Renglon(2, ("@IdTipoMembresia", id));
            return r == null ? null : new bdMembresia
            {
                IdTipoMembresia = Convert.ToInt32(r["IdTipoMembresia"]),
                IdEstatus = Entero(r, "IdEstatus"),
                Nombre = Texto(r, "Nombre"),
                Precio = Convert.ToDecimal(r["Precio"]),
                Descripcion = Texto(r, "Descripcion"),
                MaxClasesSemana = Entero(r, "MaxClasesSemana"),
                RestriccionAforo = Texto(r, "RestriccionAforo")
            };
        }

        public DataTable Consultar(string? nombre, int? idEstatus) =>
            Ejecutar(3, ("@Nombre", Valor(nombre)), ("@IdEstatus", Valor(idEstatus)));

        public bdRespuesta Eliminar(int id, int usuarioSesion) =>
            Respuesta(5, ("@IdTipoMembresia", id), ("@UsuarioSesion", usuarioSesion));

        // Membresías activas para vender suscripciones
        public DataTable ConsultarActivas() => Ejecutar(6);
    }
}
