using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;
using Microsoft.AspNetCore.Authorization;
using PureFashion.Services.Product;
using PureFashion.Models.Product;
using System.Text.Json;
using PureFashion.Common.Utils;
using PureFashion.Services.Authentication;

namespace PureFashion.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ProductController : ControllerBase
    {
        private ProductService productService;
        private AuthenticationService authService;

        public ProductController(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            productService = new ProductService(configuration, dbSettings);
            authService = new AuthenticationService(configuration, dbSettings);
        }

        [HttpGet("~/products")]
        public async Task<ActionResult<dtoListResponse<dtoProductListItem>>> GetAllProducts(string filter)
        {
            dtoListResponse<dtoProductListItem> response = await productService.GetAllProducts(JsonSerializer.Deserialize<dtoProductListFilter>(filter)!);

            if (response.error == dtoResponseMessageCodes.NOT_EXISTS)
                return BadRequest(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [HttpGet("~/products/{category}")]
        public async Task<ActionResult<dtoListResponse<dtoProductListItem>>> GetProductsByCategory(dtoProductCategory category, string filter)
        {
            dtoProductListFilter dtoFilter = JsonSerializer.Deserialize<dtoProductListFilter>(filter)!;
            dtoFilter.category = category;
            dtoListResponse<dtoProductListItem> response = await productService.GetAllProducts(dtoFilter);

            if (response.error == dtoResponseMessageCodes.NOT_EXISTS)
                return BadRequest(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [HttpGet("~/product/{productId}")]
        public async Task<ActionResult<dtoActionResponse<dtoProductEntity?>>> GetProductDetail(int productId)
        {
            dtoActionResponse<dtoProductEntity?> response = await productService.GetProductDetail(productId, await Utils.GetUser(this.HttpContext, authService));

            if (response.error == dtoResponseMessageCodes.NOT_EXISTS)
                return NotFound(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpPost("~/product")]
        public async Task<ActionResult<dtoActionResponse<bool>>> CreateProduct(dtoProduct newProduct)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            byte[] imageContent = await GetProductImageContent(newProduct.imageUrl);

            dtoProductItem product = new dtoProductItem
            {
                name = newProduct.name,
                description = newProduct.description,
                price = newProduct.price,
                category = newProduct.category.ToString(),
                image = imageContent
            };
            return Ok(await productService.CreateProduct(product, userId));
        }

        private async Task<byte[]> GetProductImageContent(string imageUrl)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(imageUrl);

            // Check if the request was successful
            response.EnsureSuccessStatusCode();

            // Read the content as a byte array
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

            return imageBytes;
        }
    }
}
