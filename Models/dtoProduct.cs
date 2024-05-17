using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PureFashion.Models.Response;
using PureFashion.Models.Review;

namespace PureFashion.Models.Product
{
    public class dtoProductListFilter : dtoListFilter
    {
        public dtoProductCategory? category { get; set; }
        public double? minPrice { get; set; }
        public double? maxPrice { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum dtoProductCategory
    {
        men,
        women,
        jewelry,
        electronics
    }

    public class dtoProduct
    {
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public double price { get; set; }
        public dtoProductCategory category { get; set; }
        public string imageUrl { get; set; } = string.Empty;
    }

    public class dtoProductData
    {
        [BsonElement("productId")]
        public int productId { get; set; }

        [BsonElement("name")]
        public string name { get; set; } = string.Empty;

        [BsonElement("price")]
        public double price { get; set; }

        [BsonElement("image")]
        public byte[]? image { get; set; }

        [BsonElement("category")]
        public string category { get; set; } = string.Empty;
    }

    public class dtoProductListItem : dtoProductData
    {
        public int reviewCount { get; set; }

        public double reviewAverage { get; set; }
    }

    public class dtoProductItem : dtoProductListItem
    {
        [BsonElement("description")]
        public string description { get; set; } = string.Empty;

        public dtoAuthor? author { get; set; }
    }

    public class dtoProductEntity : dtoProductItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonElement("authorId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string authorId { get; set; } = string.Empty;

        public List<dtoReviewEntity> reviews { get; set; } = new List<dtoReviewEntity>();
        public bool inCart { get; set; }
    }
}
