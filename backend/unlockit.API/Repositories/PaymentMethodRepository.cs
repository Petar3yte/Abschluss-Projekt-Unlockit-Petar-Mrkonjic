using Npgsql;
using unlockit.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace unlockit.API.Repositories
{
    public class PaymentMethodRepository
    {
        //Dependency Injection
        private readonly string _connectionString;

        public PaymentMethodRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<PaymentMethod>> GetAllAsync()
        {
            var methods = new List<PaymentMethod>();

            //Verbindung
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            //Datenbank Anweisung
            var sql = "SELECT paymentmethodid, name, isenabled, createdat, updatedat FROM paymentmethods ORDER BY name";
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var method = new PaymentMethod
                {
                    //Formular
                    PaymentMethodId = reader.GetInt32(reader.GetOrdinal("paymentmethodid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("isenabled")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };
                methods.Add(method);
            }
            return methods;
        }

        public async Task<IEnumerable<PaymentMethod>> GetActiveAsync()
        {
            var methods = new List<PaymentMethod>();
            //Verbindung
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            //Datenbank Anweisung
            var sql = "SELECT paymentmethodid, name, isenabled, createdat, updatedat FROM paymentmethods WHERE isenabled = true ORDER BY name";

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var method = new PaymentMethod
                {
                    //Formular
                    PaymentMethodId = reader.GetInt32(reader.GetOrdinal("paymentmethodid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("isenabled")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };
                methods.Add(method);
            }
            return methods;
        }

        public async Task<PaymentMethod?> GetByIdAsync(int id)
        {
            //Verbindung
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            //Datenbank Anweisung
            var sql = "SELECT paymentmethodid, name, isenabled, createdat, updatedat FROM paymentmethods WHERE paymentmethodid = @Id";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new PaymentMethod
                {
                    //Formular
                    PaymentMethodId = reader.GetInt32(reader.GetOrdinal("paymentmethodid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("isenabled")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };
            }
            return null;
        }

        public async Task<PaymentMethod> CreateAsync(PaymentMethod paymentMethod)
        {
            //Verbindung
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            //Datenbank Anweisung
            var sql = "INSERT INTO paymentmethods (name, isenabled) VALUES (@Name, @IsEnabled) RETURNING paymentmethodid";

            await using var command = new NpgsqlCommand(sql, connection);
            //Formular
            command.Parameters.AddWithValue("Name", paymentMethod.Name);
            command.Parameters.AddWithValue("IsEnabled", paymentMethod.IsEnabled);

            var id = (int)await command.ExecuteScalarAsync();
            paymentMethod.PaymentMethodId = id;
            return paymentMethod;
        }

        public async Task<bool> UpdateAsync(PaymentMethod paymentMethod)
        {
            //Verbindung
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            //Datenbank Anweisung
            var sql = "UPDATE paymentmethods SET name = @Name, isenabled = @IsEnabled, updatedat = @UpdatedAt WHERE paymentmethodid = @PaymentMethodId";
            await using var command = new NpgsqlCommand(sql, connection);

            //Formular
            command.Parameters.AddWithValue("Name", paymentMethod.Name);
            command.Parameters.AddWithValue("IsEnabled", paymentMethod.IsEnabled);
            command.Parameters.AddWithValue("UpdatedAt", paymentMethod.UpdatedAt);
            command.Parameters.AddWithValue("PaymentMethodId", paymentMethod.PaymentMethodId);

            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }
    }
}