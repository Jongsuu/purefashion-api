using PureFashion.Models.Cart;
using PureFashion.Models.Response;

namespace PureFashion.Services.Cart
{
    public interface ICartService
    {
        public Task<dtoListResponse<dtoProductCartData>> GetProductsFromCart(dtoPaginationFilter filter, string userId);
        public Task<dtoActionResponse<bool>> AddProductToCart(int productId, string userId);
        public Task<dtoActionResponse<bool>> RemoveProductFromCart(int productId, string userId);
    }
}
