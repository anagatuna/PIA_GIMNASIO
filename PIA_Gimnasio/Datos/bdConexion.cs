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
            // Los textos se guardan sin acentos (menos contraseñas, correos y el detalle en JSON)
            foreach (SqlParameter p in parametros)
            {
                if (p.Value is string && p.ParameterName != "@Contrasenia" && p.ParameterName != "@ContraseniaNueva"
                    && p.ParameterName != "@Correo" && p.ParameterName != "@Detalle")
                {
                    p.Value = QuitarAcentos((string)p.Value);
                }
            }

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

        // Cambia las vocales acentuadas por su vocal simple (la ñ se respeta)
        private string QuitarAcentos(string texto)
        {
            string conAcento = "áéíóúüÁÉÍÓÚÜ";
            string sinAcento = "aeiouuAEIOUU";
            for (int i = 0; i < conAcento.Length; i++)
            {
                texto = texto.Replace(conAcento[i], sinAcento[i]);
            }
            return texto;
        }
    }
}

