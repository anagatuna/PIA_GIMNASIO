using System.Data;
using Microsoft.Data.SqlClient;

namespace PIA_Gimnasio.Datos
{
    public class bdConexion
    {
        private readonly string _cadena;
        // Lee la cadena "bdConexion" de appsettings.json
        public bdConexion(IConfiguration config)
        {
            _cadena = config.GetConnectionString("bdConexion")
            ?? throw new Exception("No se encontró la cadena de conexión 'bdConexion' en appsettings.json");
        }

        // Ejecuta un SP y regresa lo que devuelve en un DataTable
        public DataTable EjecutarStoredProcedure(string nombreSP, SqlParameter[] parametros)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(_cadena))
            {
                SqlCommand cmd = new SqlCommand(nombreSP, con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddRange(parametros);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }
    }
}

