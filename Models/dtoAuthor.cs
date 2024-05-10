using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PureFashion.Models.Product
{
    public class dtoAuthor
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonElement("email")]
        public string? email { get; set; }

        [BsonElement("username")]
        public string username { get; set; } = string.Empty;
    }
}
