using PureFashion.Models.Product;
using PureFashion.Models.Response;
using PureFashion.Models.Review;

namespace PureFashion.Services.Product
{
    public interface IProductService
    {
        public Task<dtoListResponse<dtoProductListItem>> GetAllProducts(dtoProductListFilter filter);
        public Task<dtoActionResponse<dtoProductEntity?>> GetProductDetail(int productId, string? userId);
        public Task<dtoListResponse<dtoReviewItem>> GetProductReviews(int productId);
        public Task<dtoActionResponse<bool>> CreateProduct(dtoProductItem newProduct, string authorId);
        public Task<dtoActionResponse<bool>> AddProductToCart(int productId, string userId);
        public Task<dtoActionResponse<bool>> RemoveProductFromCart(int productId, string userId);
    }
}
