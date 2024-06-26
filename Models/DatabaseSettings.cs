namespace PureFashion.Models.DatabaseSettings
{
    public class PureFashionDatabaseSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;

        public string UsersCollectionName { get; set; } = null!;
        public string ProductsCollectionName { get; set; } = null!;
        public string ReviewsCollectionName { get; set; } = null!;
        public string CartCollectionName { get; set; } = null!;
        public string OrdersCollectionName { get; set; } = null!;
    }
}
