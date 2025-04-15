using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDB;

namespace LibADO.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using LibDB;

    namespace LibADO.Search
    {
        public class SearchSP
        {
            public static List<Dictionary<string, object>> sp_search_obras_com_imagem(
                string? obra, string? genre, string connectionString)
            {
                using var cn = DB.Open(connectionString);

                string filtroObra = string.IsNullOrEmpty(obra) ? "" : $"AND o.pk_obra IN (SELECT pk_obra FROM dbo.fn_search_obras('{obra}'))";
                string filtroGenero = string.IsNullOrEmpty(genre) ? "" : $"AND o.pk_obra IN (SELECT pk_obra FROM dbo.fn_search_obras_genre('{genre}'))";

                string query = $@"
                SELECT o.pk_obra, o.nome_obra, 
                    COALESCE(ir.image_path, '/images/default.jpg') AS image_path
                FROM dbo.Obra o
                LEFT JOIN dbo.ImageReferences ir ON o.fk_imagem = ir.pk_image
                WHERE 1=1 {filtroObra} {filtroGenero}
                GROUP BY o.pk_obra, o.nome_obra, ir.image_path";

                var dt = DB.GetSQLRead(cn, query);
                return DB.ToDictionary(dt);
            }
        }
    }
}

