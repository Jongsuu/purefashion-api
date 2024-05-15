using PureFashion.Models.Product;
using PureFashion.Models.Response;

namespace PureFashion.Services.Product
{
    public interface IProductService
    {
        public Task<dtoListResponse<dtoProductListItem>> GetAllProducts(dtoProductListFilter filter);
        public Task<dtoActionResponse<dtoProductEntity?>> GetProductDetail(int productId, string? userId);
        public Task<dtoActionResponse<bool>> CreateProduct(dtoProductItem newProduct, string authorId);

        public Task<dtoListResponse<dtoProductCartData>> GetProductsFromCart(dtoPaginationFilter filter, string userId);
        public Task<dtoActionResponse<bool>> AddProductToCart(int productId, string userId);
        public Task<dtoActionResponse<bool>> RemoveProductFromCart(int productId, string userId);
    }
}
