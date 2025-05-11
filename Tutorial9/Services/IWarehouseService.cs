using Tutorial9.DTOs;
using Tutorial9.Repositories;

namespace Tutorial9.Services
{
    public interface IWarehouseService
    {
        Task<int> AddProductToWarehouseAsync(AddProductRequestDto dto);
        Task<int> AddProductToWarehouseUsingProcAsync(AddProductRequestDto dto);
    }

    public class WarehouseService : IWarehouseService
    {
        private readonly IProductRepository _productRepo;
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IProductWarehouseRepository _pwRepo;

        public WarehouseService(
            IProductRepository productRepo,
            IWarehouseRepository warehouseRepo,
            IOrderRepository orderRepo,
            IProductWarehouseRepository pwRepo)
        {
            _productRepo = productRepo;
            _warehouseRepo = warehouseRepo;
            _orderRepo = orderRepo;
            _pwRepo = pwRepo;
        }

        public async Task<int> AddProductToWarehouseAsync(AddProductRequestDto dto)
        {
            if (dto.IdProduct <= 0 || dto.IdWarehouse <= 0 || dto.Amount <= 0)
                throw new ArgumentException("IdProduct, IdWarehouse and Amount must be greater than zero.");

            if (!await _productRepo.ExistsAsync(dto.IdProduct))
                throw new KeyNotFoundException($"Product {dto.IdProduct} does not exist.");
            if (!await _warehouseRepo.ExistsAsync(dto.IdWarehouse))
                throw new KeyNotFoundException($"Warehouse {dto.IdWarehouse} does not exist.");

            var order = await _orderRepo.GetMatchingOrderAsync(dto.IdProduct, dto.Amount, dto.CreatedAt);
            await _orderRepo.UpdateFulfilledAtAsync(order.IdOrder, DateTime.UtcNow);

            var product = await _productRepo.GetByIdAsync(dto.IdProduct);
            decimal totalPrice = product.Price * dto.Amount;

            var newId = await _pwRepo.AddProductWarehouseAsync(
                dto.IdWarehouse, dto.IdProduct, order.IdOrder, dto.Amount, totalPrice, DateTime.UtcNow);

            return newId;
        }

        public async Task<int> AddProductToWarehouseUsingProcAsync(AddProductRequestDto dto)
        {
            if (dto.IdProduct <= 0 || dto.IdWarehouse <= 0 || dto.Amount <= 0)
                throw new ArgumentException("IdProduct, IdWarehouse and Amount must be greater than zero.");

            if (!await _productRepo.ExistsAsync(dto.IdProduct))
                throw new KeyNotFoundException($"Product {dto.IdProduct} does not exist.");
            if (!await _warehouseRepo.ExistsAsync(dto.IdWarehouse))
                throw new KeyNotFoundException($"Warehouse {dto.IdWarehouse} does not exist.");
            int newId = await _pwRepo.AddProductWarehouseUsingProcAsync(
                dto.IdWarehouse, dto.IdProduct, dto.Amount, dto.CreatedAt);


            return newId;
        }
    }
}