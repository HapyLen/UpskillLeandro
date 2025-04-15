using System;
using System.Collections.Generic;
using System.Data;
using LibDB;
using Microsoft.Data.SqlClient;

namespace LibADO.UserRequisitions
{
    public class GetUserRequisitions
    {
        private readonly string _connectionString;

        public GetUserRequisitions(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Dictionary<string, object>> GetObrasRequisitadas(int pkLeitor)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            string query = @"
        SELECT O.pk_obra, O.nome_obra, O.fk_imagem, N.nome_nucleo,
        R.data_levantamento,
        CASE 
            WHEN DATEDIFF(DAY, R.data_levantamento, GETDATE()) >= 15 THEN 'ATRASO'
            WHEN DATEDIFF(DAY, R.data_levantamento, GETDATE()) BETWEEN 12 AND 14 THEN 'Devolução URGENTE'
            WHEN DATEDIFF(DAY, R.data_levantamento, GETDATE()) BETWEEN 10 AND 11 THEN 'Devolver em breve'
            ELSE 'Aguardando devolução'
        END AS status_entrega
        FROM dbo.Requisicao R
        INNER JOIN dbo.Obra O ON R.pk_obra = O.pk_obra
        INNER JOIN dbo.Nucleo N ON R.pk_nucleo = N.pk_nucleo
        WHERE R.pk_leitor = @PkLeitor 
        AND R.stat = 'borrowed'
        ORDER BY O.nome_obra ASC";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PkLeitor", pkLeitor);
                        Console.WriteLine($"Executando query para pkLeitor: {pkLeitor}");

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            result = DB.ToDictionary(dt);

                            Console.WriteLine($"Quantidade de registros retornados: {result.Count}");
                            Console.WriteLine("Listagem de livros requisitados:");

                            foreach (var row in result)
                            {
                                Console.WriteLine($"- Obra: {row["nome_obra"]}, Data Levantamento: {row["data_levantamento"]}, Status: {row["status_entrega"]}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao obter obras requisitadas: {ex.Message}");
                }
            }

            return result;
        }

    }
}

