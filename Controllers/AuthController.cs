using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PureFashion.Models.Auth;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Services.Authentication;
using PureFashion.Models.Response;
using PureFashion.Models.User;
using Microsoft.AspNetCore.Authorization;
using PureFashion.Common.Utils;

namespace PureFashion.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        private AuthenticationService authService;

        public AuthController(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            authService = new AuthenticationService(configuration, dbSettings);
        }

        [HttpPost("~/login")]
        public async Task<ActionResult<dtoActionResponse<dtoUser>>> Login(dtoUserLogin userLogin)
        {
            dtoActionResponse<dtoUser> response = await authService.Login(userLogin);

            if (response.error == dtoResponseMessageCodes.NOT_EXISTS)
                return NotFound(response);

            if (response.error == dtoResponseMessageCodes.WRONG_PASSWORD)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("~/register")]
        public async Task<ActionResult<dtoActionResponse<dtoUser>>> Register(dtoUserRegister userRegister)
        {
            dtoActionResponse<dtoUser> response = await authService.Register(userRegister);

            if (response.error == dtoResponseMessageCodes.USER_EXISTS)
                return BadRequest(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION || response.error == dtoResponseMessageCodes.OPERATION_NOT_PERFORMED)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }

        [Authorize]
        [HttpDelete("~/user")]
        public async Task<ActionResult<dtoActionResponse<bool>>> DeleteAccount(string password)
        {
            string? userId = await Utils.GetUser(this.HttpContext, authService);

            if (userId == null)
                return Unauthorized();

            dtoActionResponse<bool> response = await authService.DeleteAccount(password, userId);

            if (response.error == dtoResponseMessageCodes.WRONG_PASSWORD)
                return BadRequest(response);

            if (response.error == dtoResponseMessageCodes.DATABASE_OPERATION || response.error == dtoResponseMessageCodes.OPERATION_NOT_PERFORMED)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }
    }
}
