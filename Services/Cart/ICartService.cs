using PureFashion.Models.Cart;
using PureFashion.Models.Response;

namespace PureFashion.Services.Cart
{
    public interface ICartService
    {
        public Task<dtoListResponse<dtoProductCartData>> GetProductsFromCart(dtoPaginationFilter filter, string userId);
        public Task<dtoActionResponse<int>> GetCartItemsCount(string userId);
        public Task<dtoActionResponse<bool>> AddProductToCart(string productId, int quantity, string userId);
        public Task<dtoActionResponse<bool>> RemoveProductFromCart(string productId, string userId);
    }
}
