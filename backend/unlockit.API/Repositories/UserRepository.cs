using Npgsql;
using System.Data;
using unlockit.API.Models;

namespace unlockit.API.Repositories
{
    public class UserRepository
    {
        private readonly NpgsqlConnection _connection;

        public UserRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        // Wandelt die SQL-Daten in ein User-Objekt um
        private User MapReaderToUser(NpgsqlDataReader reader)
        {
            return new User
            {
                //Formular
                UserId = reader.GetInt32(reader.GetOrdinal("UserID")),
                UserUUID = reader.GetGuid(reader.GetOrdinal("UserUUID")),
                UserName = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Role = Enum.Parse<UserRole>(reader.GetString(reader.GetOrdinal("Role"))),
                FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                Birthdate = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? null : reader.GetDateTime(reader.GetOrdinal("birthdate")),
                ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("profilepictureurl")) ? null : reader.GetString(reader.GetOrdinal("profilepictureurl")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = "SELECT * FROM Users WHERE Username = @username";
                await using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("username", username);
                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapReaderToUser(reader);
                }
                return null;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = "SELECT * FROM Users WHERE Email = @email";
                await using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("email", email);
                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapReaderToUser(reader);
                }
                return null;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = "SELECT * FROM Users;";

                await using var command = new NpgsqlCommand(sql, _connection);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(MapReaderToUser(reader));
                }
                return users;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = @"INSERT INTO users (username, passwordhash, role, email, firstname, lastname, birthdate, profilepictureurl)
                            VALUES (@Username, @PasswordHash, @Role::user_role, @Email, @FirstName, @LastName, @Birthdate, @ProfilePictureUrl)
                            RETURNING *";

                await using var command = new NpgsqlCommand(sql, _connection);
                //Formular
                command.Parameters.AddWithValue("Username", user.UserName);
                command.Parameters.AddWithValue("PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("Role", user.Role.ToString());
                command.Parameters.AddWithValue("Email", user.Email);
                command.Parameters.AddWithValue("FirstName", (object)user.FirstName ?? DBNull.Value);
                command.Parameters.AddWithValue("LastName", (object)user.LastName ?? DBNull.Value);
                command.Parameters.AddWithValue("Birthdate", (object)user.Birthdate ?? DBNull.Value);
                command.Parameters.AddWithValue("ProfilePictureUrl", (object)user.ProfilePictureUrl ?? DBNull.Value);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapReaderToUser(reader);
                }
                throw new InvalidOperationException("Could not create user.");
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userUuid)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = "DELETE FROM Users WHERE UserUUID = @userUuid";
                await using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("userUuid", userUuid);
                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> UpdateUserAsync(Guid userUuid, User userToUpdate)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sqlBuilder = new System.Text.StringBuilder("UPDATE Users SET FirstName = @FirstName, LastName = @LastName, UserName = @UserName, Email = @Email, Role = @Role::user_role, UpdatedAt = @UpdatedAt, birthdate = @Birthdate ");

                if (!string.IsNullOrEmpty(userToUpdate.PasswordHash))
                {
                    sqlBuilder.Append(", PasswordHash = @PasswordHash ");
                }

                sqlBuilder.Append("WHERE UserUUID = @UserUUID");

                await using var command = new NpgsqlCommand(sqlBuilder.ToString(), _connection);
                //Formular
                command.Parameters.AddWithValue("FirstName", (object)userToUpdate.FirstName ?? DBNull.Value);
                command.Parameters.AddWithValue("LastName", (object)userToUpdate.LastName ?? DBNull.Value);
                command.Parameters.AddWithValue("UserName", userToUpdate.UserName);
                command.Parameters.AddWithValue("Email", userToUpdate.Email);
                command.Parameters.AddWithValue("Role", userToUpdate.Role.ToString());
                command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("UserUUID", userUuid);
                command.Parameters.AddWithValue("Birthdate", (object)userToUpdate.Birthdate ?? DBNull.Value);

                //Optinonal
                if (!string.IsNullOrEmpty(userToUpdate.PasswordHash))
                {
                    command.Parameters.AddWithValue("PasswordHash", userToUpdate.PasswordHash);
                }

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<User?> GetUserByUuidAsync(Guid userUuid)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = "SELECT * FROM Users WHERE UserUUID = @userUuid";
                await using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("userUuid", userUuid);
                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapReaderToUser(reader);
                }
                return null;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> UpdateProfilePictureUrlAsync(Guid userUuid, string imageUrl)
        {
            //Datenbank Anweisung
            var sql = "UPDATE users SET profilepictureurl = @ImageUrl, updatedat = @UpdatedAt WHERE useruuid = @UserUuid";
            try
            {
                //Verbindung
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, _connection);
                //Formular
                command.Parameters.AddWithValue("ImageUrl", imageUrl);
                command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("UserUuid", userUuid);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<Address>> GetAddressesByUserUuidAsync(Guid userUuid)
        {
            var addresses = new List<Address>();

            //Datenbank Anweisung
            var sql = @"SELECT a.addressid, a.addressuuid, a.userid, a.name, a.addressline1, a.city, a.postalcode, a.country, 
                a.isdefaultshipping, a.isdefaultbilling 
                FROM addresses a
                JOIN users u ON a.userid = u.userid
                WHERE u.useruuid = @UserUuid";

            try
            {
                //Verbindung
                await _connection.OpenAsync();
                await using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("UserUuid", userUuid);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    addresses.Add(new Address
                    {
                        //Formular
                        AddressId = reader.GetInt32(reader.GetOrdinal("addressid")),
                        AddressUUID = reader.GetGuid(reader.GetOrdinal("addressuuid")),
                        UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                        Name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name")),
                        AddressLine1 = reader.GetString(reader.GetOrdinal("addressline1")),
                        City = reader.GetString(reader.GetOrdinal("city")),
                        PostalCode = reader.GetString(reader.GetOrdinal("postalcode")),
                        Country = reader.GetString(reader.GetOrdinal("country")),
                        IsDefaultShipping = reader.GetBoolean(reader.GetOrdinal("isdefaultshipping")),
                        IsDefaultBilling = reader.GetBoolean(reader.GetOrdinal("isdefaultbilling"))
                    });
                }
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
            return addresses;
        }

        public async Task<Address> CreateAddressAsync(Guid userUuid, Address address)
        {
            //Datenbank Anweisung
            var userSql = "SELECT userid FROM users WHERE useruuid = @UserUUID";
            int userId;
            try
            {
                //Verbindung
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await using (var userCmd = new NpgsqlCommand(userSql, _connection))
                {
                    userCmd.Parameters.AddWithValue("UserUUID", userUuid);
                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null) throw new InvalidOperationException("Benutzer nicht gefunden.");
                    userId = (int)result;
                }

                var sql = @"INSERT INTO addresses (userid, name, addressline1, city, postalcode, country)
                    VALUES (@UserId, @Name, @AddressLine1, @City, @PostalCode, @Country)
                    RETURNING addressid, addressuuid, name, createdat, updatedat";

                await using var command = new NpgsqlCommand(sql, _connection);
                //Formular
                command.Parameters.AddWithValue("UserId", userId);
                command.Parameters.AddWithValue("Name", (object)address.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("AddressLine1", address.AddressLine1);
                command.Parameters.AddWithValue("City", address.City);
                command.Parameters.AddWithValue("PostalCode", address.PostalCode);
                command.Parameters.AddWithValue("Country", address.Country);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    //Formular
                    address.UserId = userId;
                    address.AddressId = reader.GetInt32(reader.GetOrdinal("addressid"));
                    address.AddressUUID = reader.GetGuid(reader.GetOrdinal("addressuuid"));
                    address.Name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name"));
                    address.CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat"));
                    address.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"));
                }
                return address;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<bool> DeleteAddressAsync(Guid addressUuid, Guid userUuid)
        {
            //Datenbank Anweisung
            var sql = @"DELETE FROM addresses
                WHERE addressuuid = @AddressUuid AND userid = (
                SELECT userid FROM users WHERE useruuid = @UserUuid
                )";

            try
            {
                //Verbindung
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, _connection);
                //Formular
                command.Parameters.AddWithValue("AddressUuid", addressUuid);
                command.Parameters.AddWithValue("UserUuid", userUuid);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<bool> UpdateAddressAsync(Guid addressUuid, Guid userUuid, Address updatedAddress)
        {
            //Datenbank Anweisung
            var sql = @"UPDATE addresses
                SET name = @Name,
                    addressline1 = @AddressLine1,
                    city = @City,
                    postalcode = @PostalCode,
                    country = @Country,
                    updatedat = NOW()
                WHERE addressuuid = @AddressUuid AND userid = (
                    SELECT userid FROM users WHERE useruuid = @UserUuid
                )";

            try
            {
                //Verbindung
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, _connection);
                //Formular
                command.Parameters.AddWithValue("Name", updatedAddress.Name);
                command.Parameters.AddWithValue("AddressLine1", updatedAddress.AddressLine1);
                command.Parameters.AddWithValue("City", updatedAddress.City);
                command.Parameters.AddWithValue("PostalCode", updatedAddress.PostalCode);
                command.Parameters.AddWithValue("Country", updatedAddress.Country);
                command.Parameters.AddWithValue("AddressUuid", addressUuid);
                command.Parameters.AddWithValue("UserUuid", userUuid);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }
    }
}