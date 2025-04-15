using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDB;
using Microsoft.Data.SqlClient;

namespace LibADO.RequisitionMake
{
    public class Method
    {

        public static void ProcessRequisition(string connectionString, int pkLeitor, int pkObra, int pkNucleo)
        {
            using (SqlConnection conn = DB.Open(connectionString))
            {
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    object result = GetScalarValue(conn, @"
                SELECT COUNT(*) FROM dbo.Requisicao 
                WHERE pk_leitor = @pk_leitor 
                AND stat NOT IN ('returned')",
                        "@pk_leitor", pkLeitor, transaction);

                    int totalRequisicoes = result != null ? Convert.ToInt32(result) : 0;

                    if (totalRequisicoes >= 4)
                        throw new Exception("Usuário já possui 4 requisições ativas.");

                    if (RecordExists(conn,
                    "SELECT 1 FROM dbo.Requisicao WHERE pk_leitor = @pk_leitor AND pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo AND stat NOT IN ('returned')",
                     new Dictionary<string, object> { { "@pk_leitor", pkLeitor }, { "@pk_obra", pkObra }, { "@pk_nucleo", pkNucleo } }, transaction))
                    {
                        throw new Exception("Usuário já requisitou este livro neste núcleo e ainda não devolveu.");
                    }


                    int availableCopies = Convert.ToInt32(GetScalarValue(conn,
                        "SELECT dbo.fn_available_copies(@pk_obra, @pk_nucleo)",
                        new Dictionary<string, object> { { "@pk_obra", pkObra }, { "@pk_nucleo", pkNucleo } }, transaction));

                    if (availableCopies < 2)
                        throw new Exception("Não há cópias disponíveis para requisição.");

                    string insertQuery = "INSERT INTO dbo.Requisicao (pk_leitor, pk_obra, pk_nucleo, data_levantamento, stat) VALUES (@pk_leitor, @pk_obra, @pk_nucleo, GETDATE(), 'borrowed')";
                    ExecuteNonQuery(conn, insertQuery, new Dictionary<string, object> { { "@pk_leitor", pkLeitor }, { "@pk_obra", pkObra }, { "@pk_nucleo", pkNucleo } }, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }


        private static bool RecordExists(SqlConnection conn, string query, string paramName, object paramValue, SqlTransaction transaction)
        {
            return GetScalarValue(conn, query, paramName, paramValue, transaction) != null;
        }

        private static bool RecordExists(SqlConnection conn, string query, Dictionary<string, object> parameters, SqlTransaction transaction)
        {
            return GetScalarValue(conn, query, parameters, transaction) != null;
        }

        private static object? GetScalarValue(SqlConnection conn, string query, string paramName, object paramValue, SqlTransaction transaction)
        {
            return GetScalarValue(conn, query, new Dictionary<string, object> { { paramName, paramValue } }, transaction);
        }

        private static object? GetScalarValue(SqlConnection conn, string query, Dictionary<string, object> parameters, SqlTransaction transaction)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
                return cmd.ExecuteScalar();
            }
        }

        private static void ExecuteStoredProcedure(SqlConnection conn, string procedureName, Dictionary<string, object> parameters, SqlTransaction transaction)
        {
            using (SqlCommand cmd = new SqlCommand(procedureName, conn, transaction))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
                cmd.ExecuteNonQuery();
            }
        }

        private static void ExecuteNonQuery(SqlConnection conn, string query, Dictionary<string, object> parameters, SqlTransaction transaction)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
                cmd.ExecuteNonQuery();
            }
        }

        public static List<Dictionary<string, object>> GetBookDetails(string connectionString, int pk_obra)
        {
            List<Dictionary<string, object>> results = new();

            using (SqlConnection conn = DB.Open(connectionString))
            {
                string query = @"
            SELECT o.pk_obra, o.nome_obra, o.ISBN, o.editora, 
                   COALESCE(img.image_path, '/img/default.jpg') AS image_path, 
                   n.nome_nucleo, n.pk_nucleo, 
                   dbo.fn_available_copies(o.pk_obra, n.pk_nucleo) AS quantidade
            FROM dbo.Obra o
            LEFT JOIN dbo.ImageReferences img ON o.fk_imagem = img.pk_image
            JOIN dbo.NucleoObra no ON o.pk_obra = no.pk_obra
            JOIN dbo.Nucleo n ON no.pk_nucleo = n.pk_nucleo
            WHERE o.pk_obra = @pk_obra";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pk_obra", pk_obra);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var obra = new Dictionary<string, object>
                    {
                        { "pk_obra", reader["pk_obra"] },
                        { "nome_obra", reader["nome_obra"] },
                        { "ISBN", reader["ISBN"] },
                        { "editora", reader["editora"] },
                        { "image_path", reader["image_path"] },
                        { "nome_nucleo", reader["nome_nucleo"] },
                        { "pk_nucleo", reader["pk_nucleo"] },
                        { "quantidade", reader["quantidade"] }
                    };
                            results.Add(obra);
                        }
                    }
                }
            }

            
            return results;
        }

    }
}
