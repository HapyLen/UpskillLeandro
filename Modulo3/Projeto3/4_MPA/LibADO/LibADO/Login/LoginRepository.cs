using System.Data;
using Microsoft.Data.SqlClient;
using LibADO.Login;
using LibDB;
using System;

namespace LibADO.Login
{
    public interface LoginRepository
    {
        string ValidarLogin(string email, string senha);
        LoginModel ObterUsuarioPorEmail(string email);
    }

    public class LoginRepositoryADO : LoginRepository
    {
        private readonly string _connectionString;

        public LoginRepositoryADO(string connectionString) => _connectionString = connectionString;

        public string ValidarLogin(string email, string senha)
        {
            using var conn = DB.Open(_connectionString);


            string checkUserQuery = "SELECT stat FROM Leitor WHERE email = @Email";
            using (var checkCmd = new SqlCommand(checkUserQuery, conn))
            {
                checkCmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar) { Value = email });

                var status = checkCmd.ExecuteScalar()?.ToString();

                if (status == "Inactive")
                    return "Sua conta está inativa e não pode ser acessada.";

                if (status == "inactive")
                    return "Sua conta está suspensa devido a múltiplos atrasos nas devoluções.";
            }

            if (VerificarESuspenderLeitor(email, conn))
            {
                return "Sua conta foi suspensa por excesso de devoluções atrasadas.";
            }

            string loginQuery = @"
            SELECT pk_leitor
            FROM Leitor
            WHERE email = @Email
            AND user_password = @Senha
			AND user_role = 'USER'
            AND stat = 'active'";

            using (var command = new SqlCommand(loginQuery, conn))
            {
                command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar) { Value = email });
                command.Parameters.Add(new SqlParameter("@Senha", SqlDbType.NVarChar) { Value = senha });

                var result = command.ExecuteScalar();
                return result != null ? "OK" : "Usuário ou senha inválidos.";
            }
        }

        public LoginModel? ObterUsuarioPorEmail(string email)
        {
            using var conn = DB.Open(_connectionString);
            string query = @"
            SELECT pk_leitor, email, user_password, nome_leitor, user_role, stat 
            FROM Leitor 
            WHERE email = @Email
            AND stat = 'active'";

            using var command = new SqlCommand(query, conn);
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar) { Value = email });

            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;

            return new LoginModel
            {
                Id = reader["pk_leitor"] != DBNull.Value ? Convert.ToInt32(reader["pk_leitor"]) : 0,
                Email = reader["email"].ToString(),
                Senha = reader["user_password"].ToString(),
                Nome = reader["nome_leitor"]?.ToString() ?? "Usuário",
                UserRole = reader["user_role"]?.ToString() ?? "User",
                Status = reader["stat"]?.ToString() ?? "active"
            };
        }

        private bool VerificarESuspenderLeitor(string email, SqlConnection conn)
        {
            using var transaction = conn.BeginTransaction();

            try
            {
                
                string getUserQuery = "SELECT pk_leitor FROM Leitor WHERE email = @Email";
                int pkLeitor;

                using (var cmd = new SqlCommand(getUserQuery, conn, transaction))
                {
                    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar) { Value = email });
                    pkLeitor = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                }

                if (pkLeitor == 0)
                    return false; 

               
                string countLateReturnsQuery = @"
                SELECT COUNT(*) 
                FROM dbo.Requisicao 
                WHERE pk_leitor = @pk_leitor
                AND dbo.fn_check_overtime(data_levantamento, data_devolucao, 15) = 1";

                int lateReturns;
                using (var cmd = new SqlCommand(countLateReturnsQuery, conn, transaction))
                {
                    cmd.Parameters.Add(new SqlParameter("@pk_leitor", SqlDbType.Int) { Value = pkLeitor });
                    lateReturns = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                }

                
                if (lateReturns > 3)
                {
                    string suspendUserQuery = "UPDATE Leitor SET stat = 'inactive' WHERE pk_leitor = @pk_leitor";
                    using var suspendCmd = new SqlCommand(suspendUserQuery, conn, transaction);
                    suspendCmd.Parameters.Add(new SqlParameter("@pk_leitor", SqlDbType.Int) { Value = pkLeitor });
                    suspendCmd.ExecuteNonQuery();

                    transaction.Commit();
                    return true; 
                }

                transaction.Commit();
                return false; 
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}