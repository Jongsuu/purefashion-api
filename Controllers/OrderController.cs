using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;
using Microsoft.AspNetCore.Authorization;
using PureFashion.Common.Utils;
using PureFashion.Services.Authentication;

namespace PureFashion.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class OrderController : ControllerBase
    {
        // private CartService cartService;
        private AuthenticationService authService;

        public OrderController(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            // cartService = new CartService(configuration, dbSettings);
            authService = new AuthenticationService(configuration, dbSettings);
        }
    }
}
