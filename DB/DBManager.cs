using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace GreatWall.DB
{
    [Serializable]
    public class DBManager
    {
        private readonly string connectionString;

        public DBManager(string server, int port, string username, string password, string database)
        {
            connectionString = $"server={server};port={port};user={username};password={password};database={database}";
        }

        public bool ExecuteNonQuery(string query)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                int rowsAffected = command.ExecuteNonQuery();//쿼리문이 영향을 미친 행의 수를 반환함
                return rowsAffected > 0;
            }
        }

        public DataTable ExecuteQuery(string query)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable;
            }
        }
    }
}
