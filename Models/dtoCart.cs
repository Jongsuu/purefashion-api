using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PureFashion.Models.Product;

namespace PureFashion.Models.Cart
{
    public class dtoProductCartData : dtoProductData
    {
        public DateTime addedDate { get; set; }
    }

    public class dtoCartEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonElement("product")]
        public dtoProductData product { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? userId { get; set; }

        [BsonElement("addedDate")]
        public DateTime addedDate { get; set; }
    }
}
