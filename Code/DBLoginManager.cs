using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace lr9.Code
{
    public class DBLoginManager : Singleton<DBLoginManager>
    {
        private NpgsqlConnection connection;

        public DBLoginManager()
        {
            connection = new NpgsqlConnection();
        }

        public async Task<bool> TryConnect(string ip, string port, string database, string username, string password)
        {
            try
            {
                connection.ConnectionString = $"Host={ip};Port={port};Database={database};Username={username};Password={password}";
                await connection.OpenAsync();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ошибка подключения:\n{e.Message}");
                return false;
            }
        }
        public NpgsqlConnection GetConnection()
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();

            return connection;
        }
    }
}
