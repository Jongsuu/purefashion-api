using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PureFashion.Models.Cart
{
    public class dtoCartEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonElement("productId")]
        public int productId { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? userId { get; set; }
    }
}
