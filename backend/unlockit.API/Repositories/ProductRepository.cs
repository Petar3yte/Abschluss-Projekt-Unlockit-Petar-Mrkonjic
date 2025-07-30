using Npgsql;
using unlockit.API.Models.ProductContext;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using unlockit.API.DTOs.Product;
using unlockit_API.DTOs.Order;
using unlockit.API.Models;

namespace unlockit.API.Repositories
{
    public class ProductRepository
    {
        //Dependency Injection
        private readonly NpgsqlConnection _connection;

        public ProductRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<Product> CreateProductAsync(Product product, List<int> genreIds, List<int> platformIds)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Transaktion Start
            await using var transaction = await _connection.BeginTransactionAsync();

            //Datenbank Anweisung
            try
            {
                var productSql = @"INSERT INTO products (name, description, price, sku, categoryid, brandid, stockquantity, lowstockthreshold, isvisible, createdat, updatedat)
                           VALUES (@Name, @Description, @Price, @SKU, @CategoryId, @BrandId, @StockQuantity, @LowStockThreshold, @IsVisible, @CreatedAt, @UpdatedAt)
                           RETURNING productid, productuuid;";

                
                await using (var cmd = new NpgsqlCommand(productSql, _connection, transaction))
                {
                    //Formular
                    cmd.Parameters.AddWithValue("Name", product.Name);
                    cmd.Parameters.AddWithValue("Description", (object)product.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("Price", product.Price);
                    cmd.Parameters.AddWithValue("SKU", (object)product.SKU ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("CategoryId", product.CategoryId);
                    cmd.Parameters.AddWithValue("BrandId", (object)product.BrandId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("StockQuantity", product.StockQuantity);
                    cmd.Parameters.AddWithValue("LowStockThreshold", product.LowStockThreshold);
                    cmd.Parameters.AddWithValue("IsVisible", product.IsVisible);
                    cmd.Parameters.AddWithValue("CreatedAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);

                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            product.ProductId = reader.GetInt32(0);
                            product.ProductUUID = reader.GetGuid(1);
                        }
                    }
                }

                if (genreIds != null && genreIds.Count > 0)
                {
                    var genreSql = "INSERT INTO product_genres (productid, genreid) VALUES (@ProductId, @GenreId)";
                    foreach (var genreId in genreIds)
                    {
                        await using (var cmd = new NpgsqlCommand(genreSql, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("ProductId", product.ProductId);
                            cmd.Parameters.AddWithValue("GenreId", genreId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                if (platformIds != null && platformIds.Count > 0)
                {
                    var platformSql = "INSERT INTO product_platforms (productid, platformid) VALUES (@ProductId, @PlatformId)";
                    foreach (var platformId in platformIds)
                    {
                        await using (var cmd = new NpgsqlCommand(platformSql, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("ProductId", product.ProductId);
                            cmd.Parameters.AddWithValue("PlatformId", platformId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                //Transaktions Ende/Durchführen
                await transaction.CommitAsync();
                return product;
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

        public async Task<ProductDto?> GetProductByUuidAsync(Guid productUuid)
        {
            ProductDto? productDto = null;
            int productId = 0;

            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var mainSql = @"SELECT 
                p.productid, p.productuuid, p.name, p.description, p.price, p.stockquantity, p.categoryid,
                c.name AS categoryname, 
                b.name AS brandname 
                FROM products p
                LEFT JOIN categories c ON p.categoryid = c.categoryid
                LEFT JOIN brands b ON p.brandid = b.brandid
                WHERE p.productuuid = @ProductUUID";

                await using (var cmd = new NpgsqlCommand(mainSql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductUUID", productUuid);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            productId = reader.GetInt32(reader.GetOrdinal("productid"));
                            productDto = new ProductDto
                            {
                                //Formular
                                ProductId = productId,
                                ProductUUID = reader.GetGuid(reader.GetOrdinal("productuuid")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("stockquantity")),
                                CategoryId = reader.IsDBNull(reader.GetOrdinal("categoryid")) ? 0 : reader.GetInt32(reader.GetOrdinal("categoryid")),
                                CategoryName = reader.IsDBNull(reader.GetOrdinal("categoryname")) ? null : reader.GetString(reader.GetOrdinal("categoryname")),
                                BrandName = reader.IsDBNull(reader.GetOrdinal("brandname")) ? null : reader.GetString(reader.GetOrdinal("brandname"))
                            };
                        }
                    }
                }

                if (productDto == null)
                {
                    return null;
                }

                var genreSql = @"SELECT g.name 
                         FROM genres g 
                         JOIN product_genres pg ON g.genreid = pg.genreid 
                         WHERE pg.productid = @ProductId";

                await using (var cmd = new NpgsqlCommand(genreSql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            productDto.Genres.Add(reader.GetString(0));
                        }
                    }
                }

                var platformSql = @"SELECT p.name 
                            FROM platforms p 
                            JOIN product_platforms pp ON p.platformid = pp.platformid 
                            WHERE pp.productid = @ProductId";

                await using (var cmd = new NpgsqlCommand(platformSql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            productDto.Platforms.Add(reader.GetString(0));
                        }
                    }
                }

                var imagesSql = @"SELECT imageurl, ismainimage FROM productimages WHERE productid = @ProductId";
                await using (var cmd = new NpgsqlCommand(imagesSql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            productDto.Images.Add(new ProductImageDto
                            {
                                ImageUrl = reader.GetString(reader.GetOrdinal("imageurl")),
                                IsMainImage = reader.GetBoolean(reader.GetOrdinal("ismainimage"))
                            });
                        }
                    }
                }

                return productDto;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
        public async Task<IEnumerable<ProductSummaryDto>> GetAllProductsAsync(string? categoryName = null, string? searchTerm = null, string? platformName = null, string? genreName = null)
        {
            var productDict = new Dictionary<Guid, ProductSummaryDto>();

            //Verbindung
            await using var conn = new NpgsqlConnection(_connection.ConnectionString);
            await conn.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sqlBuilder = new System.Text.StringBuilder(@"
                    SELECT 
                    p.productuuid, p.name, p.price, p.stockquantity,
                    c.name AS categoryname,
                    (SELECT i.imageurl FROM productimages i WHERE i.productid = p.productid AND i.ismainimage = true LIMIT 1) AS MainImageUrl,
                    plat.name AS platformname,
                    gen.name AS genrename
                    FROM products p
                    LEFT JOIN categories c ON p.categoryid = c.categoryid
                    LEFT JOIN product_platforms pp ON p.productid = pp.productid
                    LEFT JOIN platforms plat ON pp.platformid = plat.platformid
                    LEFT JOIN product_genres pg ON p.productid = pg.productid
                    LEFT JOIN genres gen ON pg.genreid = gen.genreid
        ");

                //Filter
                var whereClauses = new List<string>();
                whereClauses.Add("p.isvisible = true");
                whereClauses.Add("p.is_active = true");

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    whereClauses.Add("p.productid IN (SELECT p_inner.productid FROM products p_inner JOIN categories c_inner ON p_inner.categoryid = c_inner.categoryid WHERE c_inner.name ILIKE @CategoryName)");
                }
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    whereClauses.Add("(p.name ILIKE @SearchTerm OR p.description ILIKE @SearchTerm)");
                }
                
                if (!string.IsNullOrWhiteSpace(platformName))
                {
                    whereClauses.Add("p.productid IN (SELECT pp_inner.productid FROM product_platforms pp_inner JOIN platforms plat_inner ON pp_inner.platformid = plat_inner.platformid WHERE plat_inner.name ILIKE @PlatformName)");
                }
                
                if (!string.IsNullOrWhiteSpace(genreName))
                {
                    whereClauses.Add("p.productid IN (SELECT pg_inner.productid FROM product_genres pg_inner JOIN genres gen_inner ON pg_inner.genreid = gen_inner.genreid WHERE gen_inner.name ILIKE @GenreName)");
                }

                //Filter zur DB hinzufügen
                if (whereClauses.Any())
                {
                    sqlBuilder.Append(" WHERE " + string.Join(" AND ", whereClauses));
                }

                sqlBuilder.Append(" ORDER BY p.name;");

                await using (var command = new NpgsqlCommand(sqlBuilder.ToString(), conn))
                {
                    if (!string.IsNullOrWhiteSpace(categoryName))
                    {
                        command.Parameters.AddWithValue("CategoryName", $"%{categoryName}%");
                    }
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        command.Parameters.AddWithValue("SearchTerm", $"%{searchTerm}%");
                    }
                    if (!string.IsNullOrWhiteSpace(platformName))
                    {
                        command.Parameters.AddWithValue("PlatformName", $"%{platformName}%");
                    }
                    if (!string.IsNullOrWhiteSpace(genreName))
                    {
                        command.Parameters.AddWithValue("GenreName", $"%{genreName}%");
                    }

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        //Ergebnisse zusammensetzten
                        while (await reader.ReadAsync())
                        {
                            var productUuid = reader.GetGuid(reader.GetOrdinal("productuuid"));
                            if (!productDict.TryGetValue(productUuid, out var product))
                            {
                                product = new ProductSummaryDto
                                {
                                    //Formular
                                    ProductUUID = productUuid,
                                    Name = reader.GetString(reader.GetOrdinal("name")),
                                    Price = reader.GetDecimal(reader.GetOrdinal("price")),
                                    StockQuantity = reader.GetInt32(reader.GetOrdinal("stockquantity")),
                                    CategoryName = reader.IsDBNull(reader.GetOrdinal("categoryname")) ? null : reader.GetString(reader.GetOrdinal("categoryname")),
                                    MainImageUrl = reader.IsDBNull(reader.GetOrdinal("MainImageUrl")) ? null : reader.GetString(reader.GetOrdinal("MainImageUrl"))
                                };
                                productDict.Add(productUuid, product);
                            }

                            var currentPlatformName = reader.IsDBNull(reader.GetOrdinal("platformname")) ? null : reader.GetString(reader.GetOrdinal("platformname"));
                            if (currentPlatformName != null && !product.Platforms.Contains(currentPlatformName))
                            {
                                product.Platforms.Add(currentPlatformName);
                            }

                            var currentGenreName = reader.IsDBNull(reader.GetOrdinal("genrename")) ? null : reader.GetString(reader.GetOrdinal("genrename"));
                            if (currentGenreName != null && !product.Genres.Contains(currentGenreName))
                            {
                                product.Genres.Add(currentGenreName);
                            }
                        }
                    }
                }
            }
            finally
            {
                await conn.CloseAsync();
            }
            return productDict.Values.ToList();
        }

        public async Task<bool> UpdateProductAsync(int productId, Product productToUpdate, List<int> genreIds, List<int> platformIds)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Transaktion Start
            await using var transaction = await _connection.BeginTransactionAsync();

            //Datenbank Anweisung
            try
            {
                var productSql = @"UPDATE products 
                                   SET name = @Name, description = @Description, price = @Price, stockquantity = @StockQuantity,
                                   isvisible = @IsVisible, categoryid = @CategoryId, brandid = @BrandId, updatedat = @UpdatedAt
                                   WHERE productid = @ProductId";

                await using (var cmd = new NpgsqlCommand(productSql, _connection, transaction))
                {
                    //Formular
                    cmd.Parameters.AddWithValue("Name", productToUpdate.Name);
                    cmd.Parameters.AddWithValue("Description", (object)productToUpdate.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("Price", productToUpdate.Price);
                    cmd.Parameters.AddWithValue("StockQuantity", productToUpdate.StockQuantity);
                    cmd.Parameters.AddWithValue("IsVisible", productToUpdate.IsVisible);
                    cmd.Parameters.AddWithValue("CategoryId", productToUpdate.CategoryId);
                    cmd.Parameters.AddWithValue("BrandId", (object)productToUpdate.BrandId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await cmd.ExecuteNonQueryAsync();
                }

                var deleteGenresSql = "DELETE FROM product_genres WHERE productid = @ProductId";
                await using (var cmd = new NpgsqlCommand(deleteGenresSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await cmd.ExecuteNonQueryAsync();
                }

                var deletePlatformsSql = "DELETE FROM product_platforms WHERE productid = @ProductId";
                await using (var cmd = new NpgsqlCommand(deletePlatformsSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (genreIds != null && genreIds.Count > 0)
                {
                    var genreSql = "INSERT INTO product_genres (productid, genreid) VALUES (@ProductId, @GenreId)";
                    foreach (var genreId in genreIds)
                    {
                        await using (var cmd = new NpgsqlCommand(genreSql, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("ProductId", productId);
                            cmd.Parameters.AddWithValue("GenreId", genreId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                if (platformIds != null && platformIds.Count > 0)
                {
                    var platformSql = "INSERT INTO product_platforms (productid, platformid) VALUES (@ProductId, @PlatformId)";
                    foreach (var platformId in platformIds)
                    {
                        await using (var cmd = new NpgsqlCommand(platformSql, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("ProductId", productId);
                            cmd.Parameters.AddWithValue("PlatformId", platformId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                //Transaktions Ende/Durchführen
                await transaction.CommitAsync();
                return true;
            }

            //Transaktions Ende/Rollback
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

        public async Task<bool> DeleteProductAsync(Guid productUuid)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = "UPDATE products SET is_active = false WHERE productuuid = @ProductUUID";
                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductUUID", productUuid);
                    var result = await cmd.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        #region Kategorie
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var categories = new List<Category>();
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "SELECT categoryid, categoryuuid, name, description FROM categories ORDER BY name";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(new Category
                            {
                                //Formular
                                CategoryId = reader.GetInt32(0),
                                CategoryUUID = reader.GetGuid(1),
                                Name = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
            return categories;
        }

        public async Task<Category> CreateCategoryAsync(Category newCategory)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "INSERT INTO categories (name, description) VALUES (@Name, @Description) RETURNING categoryid, categoryuuid, createdat, updatedat";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    //Formular
                    command.Parameters.AddWithValue("Name", newCategory.Name);
                    command.Parameters.AddWithValue("Description", (object)newCategory.Description ?? DBNull.Value);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            //Formular
                            newCategory.CategoryId = reader.GetInt32(0);
                            newCategory.CategoryUUID = reader.GetGuid(1);
                            newCategory.CreatedAt = reader.GetDateTime(2);
                            newCategory.UpdatedAt = reader.GetDateTime(3);
                        }
                    }
                }
                return newCategory;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "DELETE FROM categories WHERE categoryid = @CategoryId";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("CategoryId", categoryId);
                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<bool> UpdateCategoryAsync(int categoryId, Category category)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "UPDATE categories SET name = @Name, description = @Description, updatedat = NOW() WHERE categoryid = @CategoryId";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    //Formular
                    command.Parameters.AddWithValue("Name", category.Name);
                    command.Parameters.AddWithValue("Description", (object)category.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("CategoryId", categoryId);
                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
        #endregion

        #region Genre & Plattform
        public async Task<IEnumerable<Platform>> GetAllPlatformsAsync()
        {
            var platforms = new List<Platform>();
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "SELECT platformid, name FROM platforms ORDER BY name";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            platforms.Add(new Platform { PlatformId = reader.GetInt32(0), Name = reader.GetString(1) });
                        }
                    }
                }
            }
            finally { await _connection.CloseAsync(); }
            return platforms;
        }

        public async Task<IEnumerable<Genre>> GetAllGenresAsync()
        {
            var genres = new List<Genre>();
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "SELECT genreid, name FROM genres ORDER BY name";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            genres.Add(new Genre { GenreId = reader.GetInt32(0), Name = reader.GetString(1) });
                        }
                    }
                }
            }
            finally { await _connection.CloseAsync(); }
            return genres;
        }

        public async Task<Platform> CreatePlatformAsync(Platform newPlatform)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "INSERT INTO platforms (name) VALUES (@Name) RETURNING platformid";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("Name", newPlatform.Name);
                    newPlatform.PlatformId = (int)await command.ExecuteScalarAsync();
                }
            }
            finally { await _connection.CloseAsync(); }
            return newPlatform;
        }

        public async Task<bool> DeletePlatformAsync(int platformId)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "DELETE FROM platforms WHERE platformid = @PlatformId";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("PlatformId", platformId);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            finally { await _connection.CloseAsync(); }
        }

        public async Task<Genre> CreateGenreAsync(Genre newGenre)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "INSERT INTO genres (name) VALUES (@Name) RETURNING genreid";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("Name", newGenre.Name);
                    newGenre.GenreId = (int)await command.ExecuteScalarAsync();
                }
            }
            finally { await _connection.CloseAsync(); }
            return newGenre;
        }

        public async Task<bool> DeleteGenreAsync(int genreId)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "DELETE FROM genres WHERE genreid = @GenreId";
                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("GenreId", genreId);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            finally { await _connection.CloseAsync(); }
        }
        #endregion

        #region Produkt-Bild
        public async Task<ProductImage> AddImageToProductAsync(int productId, string imageUrl)
        {
            var productImage = new ProductImage { ProductId = productId, ImageUrl = imageUrl };
            //Verbindung
            await _connection.OpenAsync();
            //Datenbank Anweisung
            try
            {
                var checkSql = "SELECT COUNT(*) FROM productimages WHERE productid = @ProductId";
                long imageCount;
                await using (var checkCmd = new NpgsqlCommand(checkSql, _connection))
                {
                    checkCmd.Parameters.AddWithValue("ProductId", productId);
                    imageCount = (long)await checkCmd.ExecuteScalarAsync();
                }
                productImage.IsMainImage = (imageCount == 0);

                var insertSql = "INSERT INTO productimages (productid, imageurl, ismainimage) VALUES (@ProductId, @ImageUrl, @IsMainImage) RETURNING productimageid, productimageuuid";
                await using (var cmd = new NpgsqlCommand(insertSql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductId", productImage.ProductId);
                    cmd.Parameters.AddWithValue("ImageUrl", productImage.ImageUrl);
                    cmd.Parameters.AddWithValue("IsMainImage", productImage.IsMainImage);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            productImage.ProductImageId = reader.GetInt32(0);
                            productImage.ProductImageUUID = reader.GetGuid(1);
                        }
                    }
                }
            }
            finally { await _connection.CloseAsync(); }
            return productImage;
        }

        public async Task<List<ProductImage>> GetImagesForProductAsync(int productId)
        {
            var images = new List<ProductImage>();
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "SELECT productimageid, productimageuuid, productid, imageurl, ismainimage FROM productimages WHERE productid = @ProductId";
                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            images.Add(new ProductImage
                            {
                                ProductImageId = reader.GetInt32(0),
                                ProductImageUUID = reader.GetGuid(1),
                                ProductId = reader.GetInt32(2),
                                ImageUrl = reader.GetString(3),
                                IsMainImage = reader.GetBoolean(4)
                            });
                        }
                    }
                }
            }
            finally { await _connection.CloseAsync(); }
            return images;
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Datenbank Anweisung
            try
            {
                var sql = "DELETE FROM productimages WHERE productimageid = @ImageId";
                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("ImageId", imageId);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
            finally { await _connection.CloseAsync(); }
        }

        public async Task<bool> SetMainImageAsync(int imageId)
        {
            //Verbindung
            await _connection.OpenAsync();

            //Transaktion Start
            await using var transaction = await _connection.BeginTransactionAsync();

            //Datenbank Anweisung
            try
            {
                int productId;
                var getProductIdSql = "SELECT productid FROM productimages WHERE productimageid = @ImageId";
                await using (var cmd = new NpgsqlCommand(getProductIdSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("ImageId", imageId);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null) return false;
                    productId = (int)result;
                }

                var resetSql = "UPDATE productimages SET ismainimage = false WHERE productid = @ProductId";
                await using (var cmd = new NpgsqlCommand(resetSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("ProductId", productId);
                    await cmd.ExecuteNonQueryAsync();
                }

                var setSql = "UPDATE productimages SET ismainimage = true WHERE productimageid = @ImageId";
                await using (var cmd = new NpgsqlCommand(setSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("ImageId", imageId);
                    await cmd.ExecuteNonQueryAsync();
                }

                //Transaktions Ende/Durchführen
                await transaction.CommitAsync();
                return true;
            }

            //Transaktions Ende/Rollbach
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
        #endregion

        public async Task<IEnumerable<ProductSummaryDto>> GetProductsByUuidsAsync(List<Guid> uuids)
        {
            var products = new List<ProductSummaryDto>();

            //Verbindung
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            //Datenbank Anweisung
            try
            {
                var sql = @"SELECT 
                        p.productuuid, 
                        p.name, 
                        p.price,
                        (SELECT i.imageurl FROM productimages i WHERE i.productid = p.productid AND i.ismainimage = true LIMIT 1) AS MainImageUrl
                        FROM products p
                        WHERE p.productuuid = ANY(@Uuids)";

                await using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("Uuids", uuids);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new ProductSummaryDto
                            {
                                //Formular
                                ProductUUID = reader.GetGuid(reader.GetOrdinal("productuuid")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                                MainImageUrl = reader.IsDBNull(reader.GetOrdinal("mainimageurl")) ? null : reader.GetString(reader.GetOrdinal("mainimageurl"))
                            });
                        }
                    }
                }
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
            return products;
        }
        

        public async Task<int?> AddStockAsync(Guid productUuid, int quantityToAdd)
        {
            //Datenbank Anweisung
            var sql = @"
            UPDATE products
            SET stockquantity = stockquantity + @QuantityToAdd
            WHERE productuuid = @ProductUuid
            RETURNING stockquantity";

            try
            {
                //Verbindung
                await _connection.OpenAsync();
                using (var command = new NpgsqlCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("QuantityToAdd", quantityToAdd);
                    command.Parameters.AddWithValue("ProductUuid", productUuid);
                    var newStock = await command.ExecuteScalarAsync();
                    return (int?)newStock;
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
        public async Task<decimal> ProcessDeliveryAndGetTotalCostAsync(List<DeliveryItemDto> deliveryItems)
        {
            decimal totalCost = 0;

            //Verbindung
            await _connection.OpenAsync();

            try
            {
                foreach (var item in deliveryItems)
                {
                    if (item.Quantity <= 0 || item.CostPerItem < 0)
                    {
                        continue;
                    }

                    //Datenbank Anweisung
                    var sql = "UPDATE products SET stockquantity = stockquantity + @Quantity WHERE productuuid = @ProductUuid";
                    await using (var command = new NpgsqlCommand(sql, _connection))
                    {
                        command.Parameters.AddWithValue("Quantity", item.Quantity);
                        command.Parameters.AddWithValue("ProductUuid", item.ProductUuid);
                        await command.ExecuteNonQueryAsync();
                    }
                    totalCost += item.Quantity * item.CostPerItem;
                }
                return totalCost;
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetRecentOrdersForProductAsync(Guid productUuid)
        {
            var ordersList = new List<OrderSummaryDto>();
            //Datenbank Anweisung
            var sql = @"
                SELECT
                o.orderuuid,
                COALESCE(u.firstname || ' ' || u.lastname, u.username) AS CustomerName,
                o.orderdate,
                o.totalamount,
                o.orderstatus::text AS OrderStatus
                FROM orders o
                JOIN users u ON o.userid = u.userid
                JOIN orderitems oi ON o.orderid = oi.orderid
                JOIN products p ON oi.productid = p.productid
                WHERE p.productuuid = @ProductUuid
                ORDER BY o.orderdate DESC 
                LIMIT 5";

            //Verbindung
            try
            {
                await _connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("ProductUuid", productUuid);

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
        public async Task<IEnumerable<ProductDto>> GetAllForAdminAsync()
        {
            var productDict = new Dictionary<Guid, ProductDto>();

            //Verbindung
            await using var conn = new NpgsqlConnection(_connection.ConnectionString);
            await conn.OpenAsync();

            //Datenbank Anweisung
            var sql = @"
                SELECT 
                p.productid, p.productuuid, p.name, p.price, p.stockquantity, p.is_active,
                c.name AS categoryname,
                plat.name AS platformname,
                gen.name AS genrename
                FROM products p
                LEFT JOIN categories c ON p.categoryid = c.categoryid
                LEFT JOIN product_platforms pp ON p.productid = pp.productid
                LEFT JOIN platforms plat ON pp.platformid = plat.platformid
                LEFT JOIN product_genres pg ON p.productid = pg.productid
                LEFT JOIN genres gen ON pg.genreid = gen.genreid
                ORDER BY p.name;
                ";

            await using (var command = new NpgsqlCommand(sql, conn))
            {
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var productUuid = reader.GetGuid(reader.GetOrdinal("productuuid"));
                        if (!productDict.TryGetValue(productUuid, out var product))
                        {
                            product = new ProductDto
                            {
                                //Formular
                                ProductUUID = productUuid,
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("stockquantity")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                                CategoryName = reader.IsDBNull(reader.GetOrdinal("categoryname")) ? "N/A" : reader.GetString(reader.GetOrdinal("categoryname")),
                                Platforms = new List<string>(),
                                Genres = new List<string>()
                            };
                            productDict.Add(productUuid, product);
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("platformname")))
                        {
                            var platform = reader.GetString(reader.GetOrdinal("platformname"));
                            if (!product.Platforms.Contains(platform))
                            {
                                product.Platforms.Add(platform);
                            }
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("genrename")))
                        {
                            var genre = reader.GetString(reader.GetOrdinal("genrename"));
                            if (!product.Genres.Contains(genre))
                            {
                                product.Genres.Add(genre);
                            }
                        }
                    }
                }
            }
            return productDict.Values;
        }

        public async Task<bool> ReactivateProductAsync(Guid productUuid)
        {
            try
            {
                //Verbindung
                await _connection.OpenAsync();

                //Datenbank Anweisung
                var sql = "UPDATE products SET is_active = true WHERE productuuid = @ProductUUID";
                await using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("ProductUUID", productUuid);
                    var result = await cmd.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

    }
}