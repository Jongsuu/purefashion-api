using PureFashion.Models.Order;
using PureFashion.Models.Response;

namespace PureFashion.Services.Order
{
    public interface IOrderService
    {
        public Task<dtoListResponse<dtoOrderListItem>> GetOrders(dtoPaginationFilter filter, string userId);
        public Task<dtoActionResponse<dtoOrderListItem>> GetOrderDetail(string orderId, string userId);
        public Task<dtoActionResponse<bool>> CreateOrder(List<dtoOrderProduct> products, string userId);
        public Task<dtoActionResponse<bool>> CancelOrder(string orderId, string userId);
    }
}
