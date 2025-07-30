using Npgsql;
using unlockit.API.DTOs;
using System.Threading.Tasks;
using unlockit.API.DTOs.Financial_Billing;
using System.Data.Common;
using unlockit.API.Models;

namespace unlockit.API.Repositories
{
    public class BillingRepository
    {
        //Dependency Injection 
        private readonly string _connectionString;

        public BillingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<FinancialSummaryDto> GetFinancialSummary(int year, int month)
        {
            //Formular
            var summary = new FinancialSummaryDto { Year = year, Month = month };

            //Datenbank Anweisung
            const string sql = @"
                SELECT
                    COALESCE(SUM(CASE WHEN type = 'Einnahme' THEN amount ELSE 0 END), 0) AS TotalIncome,
                    COALESCE(SUM(CASE WHEN type = 'Ausgabe' THEN amount ELSE 0 END), 0) AS TotalExpenses
                FROM public.transactions
                WHERE EXTRACT(YEAR FROM transactiondate) = @Year
                  AND EXTRACT(MONTH FROM transactiondate) = @Month;
            ";

            //Verbindung
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new NpgsqlCommand(sql, connection))
                {
                    //Daten hinzufügen
                    command.Parameters.AddWithValue("@Year", year);
                    command.Parameters.AddWithValue("@Month", month);

                    //Daten lesen
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            summary.TotalIncome = reader.GetDecimal(reader.GetOrdinal("TotalIncome"));
                            summary.TotalExpenses = reader.GetDecimal(reader.GetOrdinal("TotalExpenses"));
                        }
                    }
                }
            }

            return summary;
        }

        public async Task<OverallFinancialSummaryDto> GetOverallFinancialSummary()
        {
            //Formular
            var summary = new OverallFinancialSummaryDto();

            //Datenbank Anweisung
            const string sql = @"
                SELECT
                    COALESCE(SUM(CASE WHEN type = 'Einnahme' THEN amount ELSE 0 END), 0) AS TotalIncome,
                    COALESCE(SUM(CASE WHEN type = 'Ausgabe' THEN amount ELSE 0 END), 0) AS TotalExpenses
                FROM public.transactions;
            ";

            //Verbindung
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new NpgsqlCommand(sql, connection))
                {
                    //Daten lesen
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            summary.TotalIncome = reader.GetDecimal(reader.GetOrdinal("TotalIncome"));
                            summary.TotalExpenses = reader.GetDecimal(reader.GetOrdinal("TotalExpenses"));
                        }
                    }
                }
            }

            return summary;
        }

        public async Task CreateExpenseAsync(string description, decimal amount)
        {
            //Datenbank Anweisung
            var sql = @"
        INSERT INTO transactions (description, amount, type, transactiondate)
        VALUES (@Description, @Amount, 'Ausgabe', @TransactionDate)";

            //Verbindung
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = new NpgsqlCommand(sql, connection))
                {
                    //Daten hinzufügen
                    command.Parameters.AddWithValue("Description", description);
                    command.Parameters.AddWithValue("Amount", amount);
                    command.Parameters.AddWithValue("TransactionDate", DateTime.UtcNow);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<List<Transaction>> GetTransactions(string sql, NpgsqlParameter[] parameters = null)
        {
            var transactions = new List<Transaction>();

            //Verbindung
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            //Suchauftrag
            await using var command = new NpgsqlCommand(sql, connection);

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            //Daten lesen
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(new Transaction
                {
                    TransactionId = reader.GetInt32(reader.GetOrdinal("transactionid")),
                    TransactionDate = reader.GetDateTime(reader.GetOrdinal("transactiondate")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                    Type = reader.GetString(reader.GetOrdinal("type"))
                });
            }
            return transactions;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int year, int month)
        {
            //Datenbank Anweisung
            const string sql = @"
                SELECT transactionid, transactiondate, description, amount, type 
                FROM public.transactions
                WHERE EXTRACT(YEAR FROM transactiondate) = @Year 
                  AND EXTRACT(MONTH FROM transactiondate) = @Month
                ORDER BY transactiondate DESC;";

            //Formular
            var parameters = new NpgsqlParameter[]
            {
                new NpgsqlParameter("@Year", year),
                new NpgsqlParameter("@Month", month)
            };

            return await GetTransactions(sql, parameters);
        }

        public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
        {
            //Datenbank Anweisung
            const string sql = @"
                SELECT transactionid, transactiondate, description, amount, type 
                FROM public.transactions
                ORDER BY transactiondate DESC;";

            return await GetTransactions(sql);
        }
    }
    
}