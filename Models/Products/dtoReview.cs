using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PureFashion.Models.Product;

namespace PureFashion.Models.Review
{
    public class dtoReviewTotal
    {
        public int reviewCount { get; set; }
        public double reviewAverage { get; set; }
    }

    public class dtoReviewItem
    {
        [BsonElement("rating")]
        public int rating { get; set; }

        [BsonElement("title")]
        public string title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string description { get; set; } = string.Empty;

        public dtoAuthor? author { get; set; }
    }

    public class dtoReviewEntity : dtoReviewItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonElement("productId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string productId { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? userId { get; set; }
    }
}
