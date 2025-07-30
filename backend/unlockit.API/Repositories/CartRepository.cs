using Npgsql;
using System.Threading.Tasks;
using unlockit.API.Models.CartContext;
using System.Collections.Generic;
using unlockit.API.DTOs.Cart;

namespace unlockit.API.Repositories
{
    public class CartRepository
    {
        //Dependency Injection
        private readonly NpgsqlConnection _connection;

        public CartRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<Cart> GetOrCreateCartByUserIdAsync(int userId)
        {
            //Verbindung
            await _connection.OpenAsync();
            try
            {
                //Besteht Warenkorb?
                Cart cart;
                var findCartSql = "SELECT cartid FROM carts WHERE userid = @UserId";
                await using (var findCmd = new NpgsqlCommand(findCartSql, _connection))
                {
                    findCmd.Parameters.AddWithValue("UserId", userId);
                    //Suche (die Warenkorb-ID)
                    var cartIdResult = await findCmd.ExecuteScalarAsync();
                                       
                    if (cartIdResult != null)
                    {
                        cart = new Cart { CartId = (int)cartIdResult, UserId = userId };
                    }

                    //Erstelle (die Warenkorb-ID)
                    else
                    {
                        var createCartSql = "INSERT INTO carts (userid) VALUES (@UserId) RETURNING cartid";
                        await using (var createCmd = new NpgsqlCommand(createCartSql, _connection))
                        {
                            createCmd.Parameters.AddWithValue("UserId", userId);
                            var newCartId = (int)await createCmd.ExecuteScalarAsync();
                            cart = new Cart { CartId = newCartId, UserId = userId };
                        }
                    }
                }
                return cart;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<List<CartItemDto>> GetCartItemsByUserIdAsync(int userId)
        {
            //Formular
            var items = new List<CartItemDto>();

            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {                
                var sql = @"
                    SELECT p.productuuid, ci.quantity
                    FROM cart_items ci
                    JOIN carts c ON ci.cartid = c.cartid
                    JOIN products p ON ci.productid = p.productid
                    WHERE c.userid = @UserId";

                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("UserId", userId);

                    //Daten lesen
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new CartItemDto
                            {
                                ProductUuid = reader.GetGuid(0),
                                Quantity = reader.GetInt32(1)
                            });
                        }
                    }
                }
                return items;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task ClearCartAsync(int userId)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = @"DELETE FROM cart_items WHERE cartid = (SELECT cartid FROM carts WHERE userid = @UserId)";
                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task MergeLocalCartAsync(int userId, List<CartItemDto> localItems)
        {
            //Warenkorb Abfrage
            if (localItems == null || !localItems.Any()) return;

            var cart = await GetOrCreateCartByUserIdAsync(userId);

            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                foreach (var item in localItems)
                {
                    var sql = @"
                        INSERT INTO cart_items (cartid, productid, quantity)
                        SELECT @CartId, p.productid, @Quantity
                        FROM products p WHERE p.productuuid = @ProductUuid
                        ON CONFLICT (cartid, productid) DO UPDATE
                        SET quantity = cart_items.quantity + EXCLUDED.quantity";

                    await using (var cmd = new NpgsqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("CartId", cart.CartId);
                        cmd.Parameters.AddWithValue("ProductUuid", item.ProductUuid);
                        cmd.Parameters.AddWithValue("Quantity", item.Quantity);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task AddItemAsync(int userId, Guid productUuid, int quantity)
        {
            var cart = await GetOrCreateCartByUserIdAsync(userId);

            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = @"
                    INSERT INTO cart_items (cartid, productid, quantity)
                    SELECT @CartId, p.productid, @Quantity
                    FROM products p WHERE p.productuuid = @ProductUuid
                    ON CONFLICT (cartid, productid) DO UPDATE
                    SET quantity = cart_items.quantity + EXCLUDED.quantity;";

                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("CartId", cart.CartId);
                    cmd.Parameters.AddWithValue("ProductUuid", productUuid);
                    cmd.Parameters.AddWithValue("Quantity", quantity);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task UpdateItemQuantityAsync(int userId, Guid productUuid, int quantity)
        {
            var cart = await GetOrCreateCartByUserIdAsync(userId);
            if (quantity <= 0)
            {
                await RemoveItemAsync(userId, productUuid);
                return;
            }

            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = @"
                    UPDATE cart_items SET quantity = @Quantity
                    WHERE productid = (SELECT productid FROM products WHERE productuuid = @ProductUuid)
                    AND cartid = @CartId";

                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("CartId", cart.CartId);
                    cmd.Parameters.AddWithValue("ProductUuid", productUuid);
                    cmd.Parameters.AddWithValue("Quantity", quantity);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task RemoveItemAsync(int userId, Guid productUuid)
        {
            var cart = await GetOrCreateCartByUserIdAsync(userId);

            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = @"
                    DELETE FROM cart_items
                    WHERE productid = (SELECT productid FROM products WHERE productuuid = @ProductUuid)
                    AND cartid = @CartId";

                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("CartId", cart.CartId);
                    cmd.Parameters.AddWithValue("ProductUuid", productUuid);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
    }
}