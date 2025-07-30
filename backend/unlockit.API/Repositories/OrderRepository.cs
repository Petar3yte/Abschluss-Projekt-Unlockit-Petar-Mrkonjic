using Npgsql;
using NpgsqlTypes;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.Json;
using unlockit.API.DTOs.Order;
using unlockit.API.Models.OrderContext;
using unlockit.API.Models.ProductContext;
using unlockit_API.DTOs.Order;
using unlockit.API.DTOs.Cart;

namespace unlockit.API.Repositories
{
    public class OrderRepository
    {
        //Dependency Injection
        private readonly NpgsqlConnection _connection;

        public OrderRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<Order> CreateOrderAsync(Guid userUuid, Guid shippingAddressUuid, List<CreateOrderItemDto> items, string paymentMethodName)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Transaktion Start
            await using var transaction = await _connection.BeginTransactionAsync();

            //Datenbank Anweisung
            try
            {
                //Benutzer-ID
                int userId;
                var userSql = "SELECT userid FROM users WHERE useruuid = @UserUUID";
                await using (var userCmd = new NpgsqlCommand(userSql, _connection, transaction))
                {
                    userCmd.Parameters.AddWithValue("UserUUID", userUuid);
                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null) throw new InvalidOperationException("Benutzer nicht gefunden.");
                    userId = (int)result;
                }

                //Adressen-ID
                string shippingAddressJson = string.Empty;
                var addressSql = "SELECT name, addressline1, city, postalcode, country FROM addresses WHERE addressuuid = @AddressUUID";
                await using (var addrCmd = new NpgsqlCommand(addressSql, _connection, transaction))
                {
                    addrCmd.Parameters.AddWithValue("AddressUUID", shippingAddressUuid);
                    await using (var reader = await addrCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var address = new
                            {
                                //Formular
                                Name = reader.GetString(0),
                                Line1 = reader.GetString(1),
                                City = reader.GetString(2),
                                PostalCode = reader.GetString(3),
                                Country = reader.GetString(4)
                            };
                            shippingAddressJson = System.Text.Json.JsonSerializer.Serialize(address);
                        }
                        else
                        {
                            throw new InvalidOperationException("Lieferadresse nicht gefunden.");
                        }
                    }
                }

                //Artikel Prüfen
                decimal totalAmount = 0;
                var productsToUpdate = new List<(int ProductId, int Quantity, decimal Price)>();

                foreach (var item in items)
                {
                    var productSql = "SELECT productid, name, price, stockquantity FROM products WHERE productuuid = @ProductUUID FOR UPDATE";
                    await using (var prodCmd = new NpgsqlCommand(productSql, _connection, transaction))
                    {
                        prodCmd.Parameters.AddWithValue("ProductUUID", item.ProductUUID);
                        await using (var reader = await prodCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var stock = reader.GetInt32(3);
                                if (stock < item.Quantity)
                                {
                                    throw new InvalidOperationException($"Nicht genügend Lagerbestand für Produkt {reader.GetString(1)}. Verfügbar: {stock}, Benötigt: {item.Quantity}");
                                }

                                var price = reader.GetDecimal(2);
                                productsToUpdate.Add((reader.GetInt32(0), item.Quantity, price));
                                totalAmount += price * item.Quantity;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Produkt mit UUID {item.ProductUUID} nicht gefunden.");
                            }
                        }
                    }
                }

                // Bestellung
                var orderSql = @"INSERT INTO orders (userid, orderdate, shippingaddressjson, orderstatus, totalamount, paymentmethodname)
                     VALUES (@UserId, @OrderDate, @ShippingAddressJson::jsonb, @OrderStatus::order_status, @TotalAmount, @PaymentMethodName)
                     RETURNING orderid, orderuuid, orderdate, orderstatus";

                Order createdOrder;

                await using (var orderCmd = new NpgsqlCommand(orderSql, _connection, transaction))
                {
                    //Formular
                    orderCmd.Parameters.AddWithValue("UserId", userId);
                    orderCmd.Parameters.AddWithValue("OrderDate", DateTime.UtcNow);
                    orderCmd.Parameters.AddWithValue("ShippingAddressJson", shippingAddressJson);
                    orderCmd.Parameters.AddWithValue("OrderStatus", "in_Bearbeitung");
                    orderCmd.Parameters.AddWithValue("TotalAmount", totalAmount);
                    orderCmd.Parameters.AddWithValue("PaymentMethodName", paymentMethodName);

                    await using (var reader = await orderCmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        createdOrder = new Order { OrderId = reader.GetInt32(0), OrderUUID = reader.GetGuid(1) };
                    }
                }

                //Bestellte Artikel
                var orderItemSql = "INSERT INTO orderitems (orderid, productid, quantity, unitprice) VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";
                foreach (var p in productsToUpdate)
                {
                    //Formular
                    await using var itemCmd = new NpgsqlCommand(orderItemSql, _connection, transaction);
                    itemCmd.Parameters.AddWithValue("OrderId", createdOrder.OrderId);
                    itemCmd.Parameters.AddWithValue("ProductId", p.ProductId);
                    itemCmd.Parameters.AddWithValue("Quantity", p.Quantity);
                    itemCmd.Parameters.AddWithValue("UnitPrice", p.Price);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                // Lagerbestand aktualisieren
                var updateStockSql = "UPDATE products SET stockquantity = stockquantity - @Quantity WHERE productid = @ProductId";
                foreach (var p in productsToUpdate)
                {
                    await using var stockCmd = new NpgsqlCommand(updateStockSql, _connection, transaction);
                    stockCmd.Parameters.AddWithValue("Quantity", p.Quantity);
                    stockCmd.Parameters.AddWithValue("ProductId", p.ProductId);
                    await stockCmd.ExecuteNonQueryAsync();
                }

                //Finanzen (Einnahme)
                var transactionSql = @"
                    INSERT INTO transactions (orderid, transactiondate, description, amount, type)
                    VALUES (@OrderId, @TransactionDate, @Description, @Amount, 'Einnahme'::transaction_type)";

                await using (var transactionCmd = new NpgsqlCommand(transactionSql, _connection, transaction))
                {
                    transactionCmd.Parameters.AddWithValue("OrderId", createdOrder.OrderId);
                    transactionCmd.Parameters.AddWithValue("TransactionDate", DateTime.UtcNow);
                    transactionCmd.Parameters.AddWithValue("Description", $"Bestellung {createdOrder.OrderUUID}");
                    transactionCmd.Parameters.AddWithValue("Amount", totalAmount);
                    await transactionCmd.ExecuteNonQueryAsync();
                }

                //Transaktion Ende/Durchführen
                await transaction.CommitAsync();
                return createdOrder;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        //Bestellhistorie
        public async Task<IEnumerable<Order>> GetOrdersByUserAsync(Guid userUuid, int? limit)
        {
            
            var orderDictionary = new Dictionary<int, Order>();

            try
            {
                //Verbindung
                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }

                //mit Parameter
                string sql;
                if (limit.HasValue)
                {
                    sql = @"SELECT
                        o.orderid, o.orderuuid, o.orderdate, o.orderstatus, o.totalamount, o.shippingaddressjson,
                        oi.orderitemid, oi.quantity, oi.unitprice,
                        p.productid, p.productuuid, p.name AS productname
                        FROM (
                        SELECT ord.* FROM orders ord
                        JOIN users u ON ord.userid = u.userid
                        WHERE u.useruuid = @UserUuid
                        ORDER BY ord.orderdate DESC
                        LIMIT @Limit
                        ) AS o
                        JOIN orderitems oi ON o.orderid = oi.orderid
                        JOIN products p ON oi.productid = p.productid
                        ORDER BY o.orderdate DESC;";
                }

                //ohne Parameter
                else
                {
                    sql = @"SELECT 
                        o.orderid, o.orderuuid, o.orderdate, o.orderstatus, o.totalamount, o.shippingaddressjson,
                        oi.orderitemid, oi.quantity, oi.unitprice,
                        p.productid, p.productuuid, p.name AS productname
                        FROM orders o
                        JOIN users u ON o.userid = u.userid
                        JOIN orderitems oi ON o.orderid = oi.orderid
                        JOIN products p ON oi.productid = p.productid
                        WHERE u.useruuid = @UserUuid
                        ORDER BY o.orderdate DESC;";
                }
                

                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    //Verbindung
                    command.Parameters.AddWithValue("UserUuid", userUuid);

                    if (limit.HasValue)
                    {
                        command.Parameters.AddWithValue("Limit", limit.Value);
                    }

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var orderId = reader.GetInt32(reader.GetOrdinal("orderid"));

                            //Bereits vorhanden?
                            if (!orderDictionary.ContainsKey(orderId))
                            {
                                var order = new Order
                                {
                                    //Formular
                                    OrderId = orderId,
                                    OrderUUID = reader.GetGuid(reader.GetOrdinal("orderuuid")),
                                    OrderDate = reader.GetDateTime(reader.GetOrdinal("orderdate")),
                                    OrderStatus = Enum.Parse<OrderStatus>(reader.GetString(reader.GetOrdinal("orderstatus")), true),
                                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("totalamount")),
                                    ShippingAddressJson = reader.GetString(reader.GetOrdinal("shippingaddressjson")),
                                    Items = new List<OrderItem>()
                                };
                                orderDictionary.Add(orderId, order);
                            }

                            var orderItem = new OrderItem
                            {
                                //Formular
                                OrderItemId = reader.GetInt32(reader.GetOrdinal("orderitemid")),
                                OrderId = orderId,
                                ProductId = reader.GetInt32(reader.GetOrdinal("productid")),
                                Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                                UnitPrice = reader.GetDecimal(reader.GetOrdinal("unitprice")),
                                Product = new Product { ProductUUID = reader.GetGuid(reader.GetOrdinal("productuuid")), Name = reader.GetString(reader.GetOrdinal("productname")) }
                            };
                            orderDictionary[orderId].Items.Add(orderItem);
                        }
                    }
                }
                return orderDictionary.Values.OrderByDescending(o => o.OrderDate).ToList();
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> CancelOrderAsync(Guid orderUuid, Guid userUuid)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = @"UPDATE orders
                    SET orderstatus = @NewStatus
                    FROM users
                    WHERE orders.userid = users.userid
                      AND orders.orderuuid = @OrderUuid
                      AND users.useruuid = @UserUuid
                      AND orders.orderstatus NOT IN ('Versendet', 'Storniert')";

                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter("NewStatus", NpgsqlDbType.Unknown) { Value = "Storniert" });
                    command.Parameters.AddWithValue("OrderUuid", orderUuid);
                    command.Parameters.AddWithValue("UserUuid", userUuid);

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetAllOrdersAsync()
        {
            var ordersList = new List<OrderSummaryDto>();

            //Datenbank Anweisung
            var sql = @"
                SELECT
                o.orderuuid,
                u.firstname || ' ' || u.lastname AS CustomerName,
                o.orderdate,
                o.totalamount,
                o.orderstatus::text AS OrderStatus
                FROM orders o
                JOIN users u ON o.userid = u.userid
                ORDER BY o.orderdate DESC";

            try
            {
                //Verbindung
                await _connection.OpenAsync();

                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var orderSummary = new OrderSummaryDto
                            {
                                //Formular
                                OrderUUID = reader.GetGuid(reader.GetOrdinal("orderuuid")),
                                CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("orderdate")),
                                TotalAmount = reader.GetDecimal(reader.GetOrdinal("totalamount")),
                                OrderStatus = reader.GetString(reader.GetOrdinal("OrderStatus"))
                            };
                            ordersList.Add(orderSummary);
                        }
                    }
                }
                return ordersList;
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<OrderDto> GetOrderDetailsByUuidAsync(Guid orderUuid)
        {
            OrderDto order = null;

            //Datenbank Anweisung
            var sql = @"
                SELECT 
                o.orderuuid, o.orderdate, o.orderstatus, o.totalamount, o.shippingaddressjson,
                u.useruuid AS CustomerUUID, u.firstname || ' ' || u.lastname AS CustomerName, u.email AS CustomerEmail,
                oi.orderitemid, oi.quantity, oi.unitprice,
                p.productuuid AS ProductUUID, p.name AS ProductName
                FROM orders o
                JOIN users u ON o.userid = u.userid
                JOIN orderitems oi ON o.orderid = oi.orderid
                JOIN products p ON oi.productid = p.productid
                WHERE o.orderuuid = @OrderUuid";

            try
            {
                //Verbindung
                await _connection.OpenAsync();
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("OrderUuid", orderUuid);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (order == null)
                            {
                                order = new OrderDto
                                {
                                    //Formular
                                    OrderUUID = reader.GetGuid(reader.GetOrdinal("orderuuid")),
                                    OrderDate = reader.GetDateTime(reader.GetOrdinal("orderdate")),
                                    OrderStatus = reader.GetString(reader.GetOrdinal("orderstatus")),
                                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("totalamount")),
                                    ShippingAddress = reader.IsDBNull(reader.GetOrdinal("shippingaddressjson")) ? null : reader.GetString(reader.GetOrdinal("shippingaddressjson")),
                                    CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                                    CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                                    Items = new List<OrderItemDto>()
                                };
                            }

                            order.Items.Add(new OrderItemDto
                            {
                                ProductUUID = reader.GetGuid(reader.GetOrdinal("ProductUUID")),
                                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                                UnitPrice = reader.GetDecimal(reader.GetOrdinal("unitprice"))
                            });
                        }
                    }
                }
                return order;
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderUuid, string newStatus)
        {
            if (!Enum.TryParse<OrderStatus>(newStatus, true, out _))
            {
                return false; 
            }

            //Datenbank Anweisung
            var sql = @"UPDATE orders 
                SET orderstatus = @NewStatus::order_status 
                WHERE orderuuid = @OrderUuid";

            try
            {
                //Verbindung
                await _connection.OpenAsync();

                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("NewStatus", newStatus);
                    command.Parameters.AddWithValue("OrderUuid", orderUuid);

                    var affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }
        public async Task<IEnumerable<OrderSummaryDto>> GetRecentOrdersByUserIdAsync(int userId, int limit = 5)
        {
            var ordersList = new List<OrderSummaryDto>();

            //Datenbank Anweisung
            var sql = @"
                SELECT o.orderuuid, u.firstname || ' ' || u.lastname AS CustomerName, o.orderdate, o.totalamount, o.orderstatus::text AS OrderStatus
                FROM orders o
                JOIN users u ON o.userid = u.userid
                WHERE o.userid = @UserId 
                ORDER BY o.orderdate DESC 
                LIMIT @Limit";

            try
            {
                //Verbindung
                await _connection.OpenAsync();
                await using var command = new NpgsqlCommand(sql, _connection);

                command.Parameters.AddWithValue("UserId", userId);
                command.Parameters.AddWithValue("Limit", limit);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    ordersList.Add(new OrderSummaryDto
                    {
                        //Formular
                        OrderUUID = reader.GetGuid(reader.GetOrdinal("orderuuid")),
                        CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("orderdate")),
                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("totalamount")),
                        OrderStatus = reader.GetString(reader.GetOrdinal("OrderStatus"))
                    });
                }
                return ordersList;
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<Order> ReorderAsync(Guid originalOrderUuid, Guid currentUserUuid)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Transaktion Start
            await using var transaction = await _connection.BeginTransactionAsync();

            try
            {
                //Benutzer idenifiziren 
                int currentUserId;
                await using (var userCmd = new NpgsqlCommand("SELECT userid FROM users WHERE useruuid = @UserUuid", _connection, transaction))
                {
                    userCmd.Parameters.AddWithValue("UserUuid", currentUserUuid);
                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null) throw new InvalidOperationException("Benutzer nicht gefunden.");
                    currentUserId = (int)result;
                }

                //Datenbank Anweisung (Bestellung prüfen)
                var originalOrderSql = @"
                    SELECT o.shippingaddressjson, oi.productid, oi.quantity
                    FROM orders o
                    JOIN orderitems oi ON o.orderid = oi.orderid
                    WHERE o.orderuuid = @OriginalOrderUuid AND o.userid = @UserId";

                var originalItems = new List<(int ProductId, int Quantity)>();
                string shippingAddressJson = null;

                //Sicherstellung (Bestelung & Benuter)
                await using (var cmd = new NpgsqlCommand(originalOrderSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("OriginalOrderUuid", originalOrderUuid);
                    cmd.Parameters.AddWithValue("UserId", currentUserId);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (shippingAddressJson == null)
                            {
                                shippingAddressJson = reader.GetString(reader.GetOrdinal("shippingaddressjson"));
                            }
                            originalItems.Add((
                                ProductId: reader.GetInt32(reader.GetOrdinal("productid")),
                                Quantity: reader.GetInt32(reader.GetOrdinal("quantity"))
                            ));
                        }
                    }
                }

                if (shippingAddressJson == null || !originalItems.Any())
                {
                    throw new InvalidOperationException("Originalbestellung nicht gefunden oder gehört nicht zum aktuellen Benutzer.");
                }

                //Artikel Prüfen
                decimal newTotalAmount = 0;
                var productDetails = new List<(int ProductId, int Quantity, decimal Price)>();

                foreach (var item in originalItems)
                {
                    var productSql = "SELECT name, price, stockquantity FROM products WHERE productid = @ProductId FOR UPDATE";
                    await using (var cmd = new NpgsqlCommand(productSql, _connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("ProductId", item.ProductId);
                        await using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync()) throw new InvalidOperationException($"Ein Produkt existiert nicht mehr.");

                            //Lagerbestand prüfen
                            var stock = reader.GetInt32(reader.GetOrdinal("stockquantity"));
                            if (stock < item.Quantity)
                            {
                                var productName = reader.GetString(reader.GetOrdinal("name"));
                                throw new InvalidOperationException($"Nicht genügend Lagerbestand für '{productName}'. Benötigt: {item.Quantity}, Verfügbar: {stock}.");
                            }

                            var price = reader.GetDecimal(reader.GetOrdinal("price"));
                            newTotalAmount += price * item.Quantity;
                            productDetails.Add((item.ProductId, item.Quantity, price));
                        }
                    }
                }
                                
                //Datenbank Anweisung (Bestellung wiederholen)
                var newOrderSql = @"
                    INSERT INTO orders (userid, orderdate, shippingaddressjson, orderstatus, totalamount, paymentmethodname)
                    VALUES (@UserId, @OrderDate, @ShippingAddressJson::jsonb, @OrderStatus::order_status, @TotalAmount, @PaymentMethodName)
                    RETURNING orderid, orderuuid, orderdate, orderstatus";

                Order newOrder;
                await using (var cmd = new NpgsqlCommand(newOrderSql, _connection, transaction))
                {
                    //Formular
                    cmd.Parameters.AddWithValue("UserId", currentUserId);
                    cmd.Parameters.AddWithValue("OrderDate", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("ShippingAddressJson", shippingAddressJson);
                    cmd.Parameters.AddWithValue("OrderStatus", OrderStatus.in_Bearbeitung.ToString());
                    cmd.Parameters.AddWithValue("TotalAmount", newTotalAmount);
                    cmd.Parameters.AddWithValue("PaymentMethodName", "Wiederbestellung");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        newOrder = new Order { OrderId = reader.GetInt32(0), OrderUUID = reader.GetGuid(1) };
                    }
                }

                
                foreach (var detail in productDetails)
                {
                    var itemSql = "INSERT INTO orderitems (orderid, productid, quantity, unitprice) VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";

                    await using (var cmd = new NpgsqlCommand(itemSql, _connection, transaction))
                    {
                        //Formular
                        cmd.Parameters.AddWithValue("OrderId", newOrder.OrderId);
                        cmd.Parameters.AddWithValue("ProductId", detail.ProductId);
                        cmd.Parameters.AddWithValue("Quantity", detail.Quantity);
                        cmd.Parameters.AddWithValue("UnitPrice", detail.Price);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    
                    //Lagerbestand Anpassung
                    var stockSql = "UPDATE products SET stockquantity = stockquantity - @Quantity WHERE productid = @ProductId";
                    await using (var cmd = new NpgsqlCommand(stockSql, _connection, transaction))
                    {
                        //Formular
                        cmd.Parameters.AddWithValue("Quantity", detail.Quantity);
                        cmd.Parameters.AddWithValue("ProductId", detail.ProductId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                //Transaktions Ende/Durchführen
                await transaction.CommitAsync();
                return newOrder;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<List<CartItemDto>> GetOrderItemsForReorderAsync(Guid orderUuid, int userId)
        {
            var items = new List<CartItemDto>();

            //Datenbank Anweisung
            var sql = @"
                SELECT p.productuuid, oi.quantity
                FROM orders o
                JOIN orderitems oi ON o.orderid = oi.orderid
                JOIN products p ON oi.productid = p.productid
                WHERE o.orderuuid = @OrderUuid AND o.userid = @UserId";

            
            await _connection.OpenAsync();
            try
            {
                //Verbindung
                await using var command = new NpgsqlCommand(sql, _connection);

                command.Parameters.AddWithValue("OrderUuid", orderUuid);
                command.Parameters.AddWithValue("UserId", userId);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new CartItemDto
                    {
                        ProductUuid = reader.GetGuid(0),
                        Quantity = reader.GetInt32(1)
                    });
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
            return items;
        }
    }
}
