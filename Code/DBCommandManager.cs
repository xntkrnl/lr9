using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;

namespace lr9.Code
{
    public class DBCommandManager : Singleton<DBCommandManager>
    {
        private NpgsqlTransaction? transaction = null;

        public async Task<bool> ExecuteCommands(string commands, MainWindow mainWindow)
        {
            mainWindow.ResultTabControl.Items.Clear();

            List<string> queries = SplitSqlStatements(commands);

            var connection = DBLoginManager.Instance.GetConnection();
            try
            {
                bool needToGoToResultsTab = false;
                int totalRows = 0;

                foreach (string query in queries)
                {
                    if (string.IsNullOrWhiteSpace(query)) continue;

                    NpgsqlCommand command = transaction != null
                        ? new NpgsqlCommand(query, connection, transaction)
                        : new NpgsqlCommand(query, connection);

                    var qLower = query.Trim().ToLower();

                    if (qLower.StartsWith("select") || qLower.StartsWith("explain"))
                    {
                        using var reader = await command.ExecuteReaderAsync();
                        DataTable table = new DataTable();
                        table.Load(reader);

                        mainWindow.CreateNewResultTab(table);
                        needToGoToResultsTab = true;
                    }
                    else if (qLower.StartsWith("begin"))
                    {
                        if (transaction != null)
                            throw new Exception("Уже есть активная транзакция");
                        transaction = await connection.BeginTransactionAsync();
                    }
                    else if (qLower.StartsWith("commit"))
                    {
                        if (transaction == null)
                            throw new Exception("Нет активной транзакции");
                        await transaction.CommitAsync();
                        transaction = null;
                    }
                    else if (qLower.StartsWith("rollback"))
                    {
                        if (transaction == null)
                            throw new Exception("Нет активной транзакции");

                        if (qLower.StartsWith("rollback to"))
                        {
                            string spName = query.Substring("rollback to".Length).TrimEnd(';', ' ');
                            await transaction.RollbackAsync(spName);
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            transaction = null;
                        }
                    }
                    else if (qLower.StartsWith("savepoint"))
                    {
                        if (transaction == null)
                            throw new Exception("Нет активной транзакции");

                        string spName = query.Substring("savepoint".Length).TrimEnd(';', ' ');
                        await transaction.SaveAsync(spName);
                    }
                    else
                    {
                        int rows = await command.ExecuteNonQueryAsync();
                        totalRows += rows;
                    }
                }

                if (totalRows > 0)
                    MessageBox.Show($"Операция успешна. Затронуто строк: {totalRows}");

                if (needToGoToResultsTab)
                    mainWindow.SelectResultTab();

                return true;
            }
            catch (Exception e)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();
                MessageBox.Show($"Ошибка SQL:\n{e.Message}");
                return false;
            }
        }
        public List<string> SplitSqlStatements(string sql)
        {
            var statements = new List<string>();
            var sb = new StringBuilder();

            bool inString = false;
            bool inLineComment = false;
            bool inBlockComment = false;
            bool inDollarString = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                char next = i + 1 < sql.Length ? sql[i + 1] : '\0';

                // $$
                if (!inString && !inLineComment && !inBlockComment && c == '$' && next == '$')
                {
                    inDollarString = !inDollarString;
                    sb.Append("$$");
                    i++;
                    continue;
                }

                if (inDollarString)
                {
                    sb.Append(c);
                    continue;
                }

                // '...'
                if (!inLineComment && !inBlockComment && c == '\'' && (i == 0 || sql[i - 1] != '\\'))
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                // --
                if (!inBlockComment && c == '-' && next == '-')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (inLineComment)
                {
                    if (c == '\n')
                        inLineComment = false;

                    continue;
                }

                // /* */
                if (!inLineComment && c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }
                    continue;
                }

                // ;
                if (c == ';')
                {
                    if (sb.Length > 0)
                        statements.Add(sb.ToString().Trim());

                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0)
                statements.Add(sb.ToString().Trim());

            return statements;
        }
    }
}

