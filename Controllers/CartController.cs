using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using PureFashion.Common.Utils;
using PureFashion.Services.Authentication;
using PureFashion.Models.Cart;
using PureFashion.Services.Cart;

namespace PureFashion.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CartController : ControllerBase
    {
        private CartService cartService;
        private AuthenticationService authService;

        public CartController(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            cartService = new CartService(configuration, dbSettings);
            authService = new AuthenticationService(configuration, dbSettings);
        }

        [Authorize]
        [HttpGet("~/products/cart")]
        public async Task<ActionResult<dtoListResponse<dtoProductCartData>>> GetProductsFromCart(string filter)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoPaginationFilter dtoFilter = JsonSerializer.Deserialize<dtoPaginationFilter>(filter)!;
            dtoListResponse<dtoProductCartData> response = await cartService.GetProductsFromCart(dtoFilter, userId);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpPost("~/product/{productId}/cart")]
        public async Task<ActionResult<dtoActionResponse<bool>>> AddProductToCart(string productId, int quantity)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            if (quantity <= 0 || quantity > 30)
            {
                return BadRequest(new dtoActionResponse<bool>
                {
                    data = false,
                    error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED,
                    message = "Quantity must be between 1 and 30"
                });
            }

            dtoActionResponse<bool> response = await cartService.AddProductToCart(productId, quantity, userId);

            if (response.error == dtoResponseMessageCodes.OPERATION_NOT_PERFORMED)
                return BadRequest(response);

            if (response.error == dtoResponseMessageCodes.NOT_EXISTS)
                return NotFound(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpGet("~/products/cart/indicator")]
        public async Task<ActionResult<dtoActionResponse<int>>> GetCartItemsCount()
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoActionResponse<int> response = await cartService.GetCartItemsCount(userId);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpDelete("~/product/{productId}/cart")]
        public async Task<ActionResult<dtoActionResponse<bool>>> RemoveProductToCart(string productId)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoActionResponse<bool> response = await cartService.RemoveProductFromCart(productId, userId);

            if (response.error == dtoResponseMessageCodes.OPERATION_NOT_PERFORMED)
                return BadRequest(response);

            if (response.error == dtoResponseMessageCodes.NOT_EXISTS)
                return NotFound(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }
    }
}
