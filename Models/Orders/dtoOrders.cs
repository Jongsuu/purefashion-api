using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PureFashion.Models.Product;

namespace PureFashion.Models.Order
{
    public class dtoOrderListItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? orderId { get; set; }

        [BsonElement("orderDate")]
        public DateTime orderDate { get; set; }

        [BsonElement("deliveryDate")]
        public DateTime deliveryDate { get; set; }

        [BsonElement("status")]
        public OrderStatus status { get; set; }

        [BsonElement("total")]
        public double total { get; set; }

        [BsonIgnore]
        public dtoOrderProducts order { get; set; }
    }

    public class dtoOrderEntity : dtoOrderListItem
    {
        [BsonElement("products")]
        public List<dtoOrderProduct> products { get; set; } = new List<dtoOrderProduct>();

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string userId { get; set; } = string.Empty;
    }

    public class dtoOrderProduct
    {
        [BsonElement("productId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string productId { get; set; }

        [BsonElement("quantity")]
        public int quantity { get; set; }
    }

    public class dtoOrderProducts
    {
        public List<dtoOrderListItemProduct> products { get; set; } = new List<dtoOrderListItemProduct>();
        public int productsCount { get; set; }
    }

    public class dtoOrderListItemProduct : dtoProductData
    {
        public int quantity { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderStatus
    {
        NOT_SHIPPED,
        SHIPPING,
        SHIPPED,
        IN_DELIVERY,
        DELIVERED,
        DELAYED,
        LOST
    }
}
