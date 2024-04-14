using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PureFashion.Models.User
{
    public class dtoUser
    {
        public string username { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
    }

    public class dtoUserEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonElement("username")]
        public string username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public byte[] passwordHash { get; set; }

        [BsonElement("passwordSalt")]
        public byte[] passwordSalt { get; set; }
    }
}
