using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

namespace lr9.Code
{
    public class DBLoginManager : Singleton<DBLoginManager>
    {
        private string? connectionString;

        public DBLoginManager()
        {
        }

        public async Task<bool> TryConnect(string ip, string port, string database, string username, string password)
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = ip,
                    Port = int.Parse(port),
                    Database = database,
                    Username = username,
                    Password = password
                };

                // Валидация
                var tmp = new NpgsqlConnection(builder.ConnectionString);
                await tmp.OpenAsync();
                await tmp.CloseAsync();

                connectionString = builder.ConnectionString;

                return true;
            }
            catch (Exception)
            {
                // Общее сообщение об ошибке, чтобы не раскрывать детали подключения
                MessageBox.Show("Не удалось подключиться. Проверьте хост, порт, учетные данные и сеть.");
                return false;
            }
        }

        public async Task<NpgsqlConnection> GetOpenConnectionAsync()
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Нет действительного подключения. Сначала вызовите TryConnect.");

            var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            return conn;
        }
    }
}
