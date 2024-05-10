using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using PureFashion.Models.Auth;
using PureFashion.Models.User;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;

namespace PureFashion.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<dtoUserEntity> usersCollection;

        public AuthenticationService(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            _configuration = configuration;
            MongoClient mongoClient = new MongoClient(dbSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
            usersCollection = mongoDatabase.GetCollection<dtoUserEntity>(dbSettings.Value.UsersCollectionName);
        }

        public async Task<dtoActionResponse<dtoUser>> Login(dtoUserLogin userLogin)
        {
            dtoActionResponse<dtoUser> response = new dtoActionResponse<dtoUser>();
            dtoUserEntity? user = await GetUserByEmail(userLogin.email);

            if (user is null)
            {
                response.error = dtoResponseMessageCodes.NOT_EXISTS;
                response.message = "We couldn't find a user those credentials";
            }
            else if (!VerifyPasswordHash(userLogin.password, user.passwordHash, user.passwordSalt))
            {
                response.error = dtoResponseMessageCodes.WRONG_PASSWORD;
                response.message = "The password doesn't match";
            }
            else
            {
                response.data = new dtoUser
                {
                    username = user.username,
                    email = user.email,
                    token = CreateToken(user)
                };
            }

            return response;
        }

        public async Task<dtoActionResponse<dtoUser>> Register(dtoUserRegister userRegister)
        {
            dtoActionResponse<dtoUser> response = new dtoActionResponse<dtoUser>();

            if (await UserExists(userRegister.email))
            {
                response.error = dtoResponseMessageCodes.USER_EXISTS;
                response.message = "User already exists";
                return response;
            }

            CreatePasswordHash(userRegister.password, out byte[] passwordHash, out byte[] passwordSalt);

            dtoUserEntity user = new dtoUserEntity
            {
                username = userRegister.username,
                email = userRegister.email,
                passwordHash = passwordHash,
                passwordSalt = passwordSalt,
            };
            dtoActionResponse<string?> addUserResponse = await AddUser(user);

            if (addUserResponse.error != null)
            {
                response.error = addUserResponse.error;
                response.message = addUserResponse.message;
                return response;
            }

            user.id = (string)addUserResponse.data!;
            response.data = new dtoUser
            {
                username = user.username,
                email = user.email,
                token = CreateToken(user)
            };

            return response;
        }

        public async Task<dtoUserEntity?> GetUserById(string? id)
        {
            if (id == null)
                return null;

            dtoUserEntity? userEntity;

            try
            {
                userEntity = await usersCollection
                    .Find(item => item.id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                userEntity = null;
            }

            return userEntity;
        }

        private async Task<dtoActionResponse<string?>> AddUser(dtoUserEntity user)
        {
            dtoActionResponse<string?> response = new dtoActionResponse<string?>();
            try
            {
                await usersCollection.InsertOneAsync(user);

                if (user.id == null)
                {
                    response.error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED;
                    response.message = "User couldn't be stored";
                }
                else
                    response.data = user.id;
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "Error registering new user";
            }

            return response;
        }

        private async Task<dtoUserEntity?> GetUserByEmail(string email)
        {
            dtoUserEntity? userEntity = null;
            string emailL = email.ToLower();

            try
            {
                userEntity = await usersCollection
                    .Find(item => item.email.ToLower() == emailL)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                userEntity = null;
            }

            return userEntity;
        }

        private async Task<bool> UserExists(string email)
        {
            bool exists = true;
            string emailL = email.ToLower();

            try
            {
                dtoUserEntity? result = await usersCollection
                    .Find(item => item.email.ToLower() == emailL)
                    .FirstOrDefaultAsync();
                exists = result != null;
            }
            catch (Exception)
            {
                exists = true;
            }

            return exists;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (HMACSHA512 hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            bool correct;

            using (HMACSHA512 hmac = new HMACSHA512(passwordSalt))
            {
                byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                correct = computedHash.SequenceEqual(passwordHash);
            }

            return correct;
        }

        private string CreateToken(dtoUserEntity user)
        {
            List<Claim> claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, user.id!),
                new Claim(ClaimTypes.Name, user.username)
            };

            string? appSettingsToken = _configuration.GetSection("AppSettings:Token").Value;

            if (appSettingsToken is null)
                throw new Exception("AppSettings Token is null!");

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettingsToken));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
