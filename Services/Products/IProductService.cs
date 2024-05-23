using PureFashion.Models.Product;
using PureFashion.Models.Response;

namespace PureFashion.Services.Product
{
    public interface IProductService
    {
        public Task<dtoListResponse<dtoProductListItem>> GetAllProducts(dtoProductListFilter filter);
        public Task<dtoActionResponse<dtoProductEntity?>> GetProductDetail(string productId, string? userId);
        public Task<dtoActionResponse<bool>> CreateProduct(dtoProductItem newProduct, string authorId);
    }
}
