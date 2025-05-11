using Tutorial9.Model;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Tutorial9.Repositories
{
    public interface IProductRepository
    {
        Task<bool> ExistsAsync(int idProduct);
        Task<Product> GetByIdAsync(int idProduct);
    }

    public class SqlProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        public SqlProductRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("WarehouseDb");
        }

        public async Task<bool> ExistsAsync(int idProduct)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM Product WHERE IdProduct = @id", conn);
            cmd.Parameters.AddWithValue("@id", idProduct);
            var result = (int)await cmd.ExecuteScalarAsync();
            return result > 0;
        }

        public async Task<Product> GetByIdAsync(int idProduct)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT IdProduct, Name, Description, Price FROM Product WHERE IdProduct = @id", conn);
            cmd.Parameters.AddWithValue("@id", idProduct);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Product {
                    IdProduct = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    Price = reader.GetDecimal(3)
                };
            }
            else
            {
                throw new KeyNotFoundException($"Product {idProduct} not found.");
            }
        }
    }

    public interface IWarehouseRepository
    {
        Task<bool> ExistsAsync(int idWarehouse);
    }

    public class SqlWarehouseRepository : IWarehouseRepository
    {
        private readonly string _connectionString;
        public SqlWarehouseRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("WarehouseDb");
        }

        public async Task<bool> ExistsAsync(int idWarehouse)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM Warehouse WHERE IdWarehouse = @id", conn);
            cmd.Parameters.AddWithValue("@id", idWarehouse);
            var result = (int)await cmd.ExecuteScalarAsync();
            return result > 0;
        }
    }

    public interface IOrderRepository
    {
        Task<Order> GetMatchingOrderAsync(int idProduct, int amount, DateTime createdBefore);
        Task UpdateFulfilledAtAsync(int idOrder, DateTime fulfilledAt);
    }

    public class SqlOrderRepository : IOrderRepository
    {
        private readonly string _connectionString;
        public SqlOrderRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("ConnectionString");
        }

        public async Task<Order> GetMatchingOrderAsync(int idProduct, int amount, DateTime createdBefore)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"
            SELECT TOP 1 IdOrder, IdProduct, Amount, CreatedAt, FulfilledAt 
            FROM [Order] o
            WHERE o.IdProduct = @pid 
                AND o.Amount = @amt 
                AND o.CreatedAt < @created 
                AND NOT EXISTS (
                    SELECT 1 FROM Product_Warehouse pw WHERE pw.IdOrder = o.IdOrder
                    )
            ORDER BY o.CreatedAt ASC;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", idProduct);
            cmd.Parameters.AddWithValue("@amt", amount);
            cmd.Parameters.AddWithValue("@created", createdBefore);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Order {
                    IdOrder = reader.GetInt32(0),
                    IdProduct = reader.GetInt32(1),
                    Amount = reader.GetInt32(2),
                    CreatedAt = reader.GetDateTime(3),
                    FulfilledAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
                };
            }
            else
            {
                throw new InvalidOperationException("No matching unfulfilled order found.");
            }
        }

        public async Task UpdateFulfilledAtAsync(int idOrder, DateTime fulfilledAt)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "UPDATE [Order] SET FulfilledAt = @fulfilled WHERE IdOrder = @id", conn);
            cmd.Parameters.AddWithValue("@fulfilled", fulfilledAt);
            cmd.Parameters.AddWithValue("@id", idOrder);
            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Order {idOrder} not found or not updated.");
            }
        }
    }

    public interface IProductWarehouseRepository
    {
        Task<int> AddProductWarehouseAsync(int idWarehouse, int idProduct, int idOrder, int amount, decimal price, DateTime createdAt);
        Task<int> AddProductWarehouseUsingProcAsync(int idWarehouse, int idProduct, int amount, DateTime createdAt);
    }

    public class SqlProductWarehouseRepository : IProductWarehouseRepository
    {
        private readonly string _connectionString;
        public SqlProductWarehouseRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("WarehouseDb");
        }

        public async Task<int> AddProductWarehouseAsync(int idWarehouse, int idProduct, int idOrder, int amount, decimal price, DateTime createdAt)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES (@wh, @prod, @ord, @amt, @price, @created);
            SELECT CAST(SCOPE_IDENTITY() AS int);", conn);
            cmd.Parameters.AddWithValue("@wh", idWarehouse);
            cmd.Parameters.AddWithValue("@prod", idProduct);
            cmd.Parameters.AddWithValue("@ord", idOrder);
            cmd.Parameters.AddWithValue("@amt", amount);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@created", createdAt);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> AddProductWarehouseUsingProcAsync(int idWarehouse, int idProduct, int amount, DateTime createdAt)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("AddProductToWarehouse", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@IdProduct", idProduct);
            cmd.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("Stored procedure did not return a new ID.");
            }
            return Convert.ToInt32(result);
        }
    }
}