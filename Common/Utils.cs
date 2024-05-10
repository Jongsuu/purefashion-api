using System.Security.Claims;
using PureFashion.Models.User;
using PureFashion.Services.Authentication;

namespace PureFashion.Common.Utils
{
    public static class Utils
    {
        public static async Task<string?> GetUser(HttpContext context, AuthenticationService authService)
        {
            string? id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            dtoUserEntity? user = await authService.GetUserById(id);

            if (user == null)
                return null;

            return user.id;
        }
    }
}
