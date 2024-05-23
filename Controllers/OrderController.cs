using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;
using Microsoft.AspNetCore.Authorization;
using PureFashion.Common.Utils;
using PureFashion.Services.Authentication;
using PureFashion.Services.Order;
using PureFashion.Models.Order;
using System.Text.Json;

namespace PureFashion.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class OrderController : ControllerBase
    {
        private OrderService orderService;
        private AuthenticationService authService;

        public OrderController(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            orderService = new OrderService(configuration, dbSettings);
            authService = new AuthenticationService(configuration, dbSettings);
        }

        [Authorize]
        [HttpGet("~/orders")]
        public async Task<ActionResult<dtoListResponse<dtoOrderListItem>>> GetOrders(string filter)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoPaginationFilter dtoFilter = JsonSerializer.Deserialize<dtoPaginationFilter>(filter)!;
            dtoListResponse<dtoOrderListItem> response = await orderService.GetOrders(dtoFilter, userId);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpGet("~/order/{orderId}")]
        public async Task<ActionResult<dtoListResponse<dtoOrderEntity>>> GetOrderDetail(string orderId)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoActionResponse<dtoOrderEntity> response = await orderService.GetOrderDetail(orderId, userId);

            if (response.error == dtoResponseMessageCodes.NOT_EXISTS)
                return NotFound(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpPost("~/order")]
        public async Task<ActionResult<dtoActionResponse<bool>>> CreateOrder(List<dtoOrderProduct> products)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoActionResponse<bool> response = await orderService.CreateOrder(products, userId);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION || response.error == dtoResponseMessageCodes.OPERATION_NOT_PERFORMED)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpPost("~/order/cart")]
        public async Task<ActionResult<dtoActionResponse<bool>>> CreateOrderFromCart()
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoActionResponse<bool> response = await orderService.CreateOrderFromCart(userId);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION || response.error == dtoResponseMessageCodes.OPERATION_NOT_PERFORMED)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpDelete("~/order/{orderId}")]
        public async Task<ActionResult<dtoActionResponse<bool>>> CancelOrder(string orderId)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoActionResponse<bool> response = await orderService.CancelOrder(orderId, userId);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION || response.error == dtoResponseMessageCodes.OPERATION_NOT_PERFORMED)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }
    }
}
