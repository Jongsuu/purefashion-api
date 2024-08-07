using PureFashion.Models.Auth;
using PureFashion.Models.Response;
using PureFashion.Models.User;

namespace PureFashion.Services.Authentication
{
    public interface IAuthenticationService
    {
        public Task<dtoActionResponse<dtoUser>> Register(dtoUserRegister userRegister);
        public Task<dtoActionResponse<dtoUser>> Login(dtoUserLogin userLogin);
        public Task<dtoActionResponse<bool>> DeleteAccount(string password, string userId);
        public Task<dtoUserEntity?> GetUserById(string? id);
    }
}
