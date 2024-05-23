namespace PureFashion.Models.Auth
{
    public class dtoUserRegister
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
    }

    public class dtoUserLogin
    {
        public string email { get; set; }
        public string password { get; set; }
    }
}
