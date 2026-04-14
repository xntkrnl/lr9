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
        public bool safeMode;
        private List<string> whitelist = new List<string>
        {
            "select",
            "explain",
            "begin",
            "commit",
            "rollback",
            "savepoint"
        };

        public async Task<bool> ExecuteCommands(string commands, MainWindow mainWindow)
        {
            mainWindow.ResultTabControl.Items.Clear();

            List<string> queries = SplitSqlStatements(commands);

            if (safeMode)
            {
                foreach (var query in queries)
                {
                    var qLower = query.Trim().ToLower();
                    if (!whitelist.Any(w => qLower.StartsWith(w)))
                    {
                        MessageBox.Show($"Запрещенная команда в безопасном режиме: {query}");
                        return false;
                    }
                }
            }

            await using var connection = await DBLoginManager.Instance.GetOpenConnectionAsync();

            // настройки безопасного режима
            if (safeMode)
            {
                await using var setCmd = new NpgsqlCommand(
                    "SET default_transaction_read_only = on; SET statement_timeout = 10000; SET lock_timeout = 2000;", 
                    connection);
                setCmd.CommandTimeout = 5;
                await setCmd.ExecuteNonQueryAsync();
            }

            bool needToGoToResultsTab = false;
            int totalRows = 0;
            bool autoTransactionStarted = false;
            NpgsqlTransaction? transaction = null;

            try
            {
                foreach (string query in queries)
                {
                    if (string.IsNullOrWhiteSpace(query)) continue;

                    var qLower = query.Trim().ToLower();

                    if (qLower.StartsWith("begin"))
                    {
                        if (transaction != null)
                            throw new Exception("Уже есть активная транзакция");
                        transaction = await connection.BeginTransactionAsync();
                        continue;
                    }
                    else if (qLower.StartsWith("commit"))
                    {
                        if (transaction == null)
                            throw new Exception("Нет активной транзакции");
                        await transaction.CommitAsync();
                        await transaction.DisposeAsync();
                        transaction = null;
                        autoTransactionStarted = false;
                        continue;
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
                            await transaction.DisposeAsync();
                            transaction = null;
                            autoTransactionStarted = false;
                        }
                        continue;
                    }
                    else if (qLower.StartsWith("savepoint"))
                    {
                        if (transaction == null)
                            throw new Exception("Нет активной транзакции");

                        string spName = query.Substring("savepoint".Length).TrimEnd(';', ' ');
                        await transaction.SaveAsync(spName);
                        continue;
                    }

                    bool isSelectLike = qLower.StartsWith("select") || qLower.StartsWith("explain");
                    if (!isSelectLike && transaction == null && !safeMode)
                    {
                        transaction = await connection.BeginTransactionAsync();
                        autoTransactionStarted = true;
                    }

                    using var command = transaction != null
                        ? new NpgsqlCommand(query, connection, transaction)
                        : new NpgsqlCommand(query, connection);

                    command.CommandTimeout = 30;

                    if (isSelectLike)
                    {
                        using var reader = await command.ExecuteReaderAsync();
                        DataTable table = new DataTable();
                        table.Load(reader);

                        mainWindow.CreateNewResultTab(table);
                        needToGoToResultsTab = true;
                    }
                    else
                    {
                        int rows = await command.ExecuteNonQueryAsync();
                        totalRows += rows;
                    }
                }

                if (autoTransactionStarted && transaction != null)
                {
                    await transaction.CommitAsync();
                    await transaction.DisposeAsync();
                    transaction = null;
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
                {
                    try { await transaction.RollbackAsync(); } catch { }
                    await transaction.DisposeAsync();
                    transaction = null;
                }

                MessageBox.Show("Ошибка выполнения SQL. Подробности записаны в лог.");
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

