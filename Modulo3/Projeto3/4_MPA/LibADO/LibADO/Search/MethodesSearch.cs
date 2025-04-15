using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDB;

namespace LibADO.Search
{
    public class MethodesSearch
    {
        public List<string> ObterCategorias(string connectionString)
        {
            List<string> categorias = new();
            using var cn = DB.Open(connectionString);
            string query = "SELECT DISTINCT nome_genero FROM dbo.Genero ORDER BY nome_genero";

            var dt = DB.GetSQLRead(cn, query);
            foreach (var row in DB.ToDictionary(dt))
            {
                categorias.Add(row["nome_genero"].ToString());
            }
            return categorias;
        }

    }
}
