using MongoDB.Driver;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;
using PureFashion.Models.Product;
using PureFashion.Models.User;
using PureFashion.Models.Cart;
using PureFashion.Models.Order;

namespace PureFashion.Services.Order
{
    public class OrderService : IOrderService
    {
        private readonly IConfiguration _configuration;

        private readonly IMongoCollection<dtoProductEntity> productsCollection;
        private readonly IMongoCollection<dtoUserEntity> usersCollection;
        private readonly IMongoCollection<dtoCartEntity> cartCollection;
        private readonly IMongoCollection<dtoOrderEntity> ordersCollection;

        private readonly IOptions<PureFashionDatabaseSettings> _dbSettings;

        public OrderService(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            _configuration = configuration;
            _dbSettings = dbSettings;

            MongoClient mongoClient = new MongoClient(dbSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            productsCollection = mongoDatabase.GetCollection<dtoProductEntity>(dbSettings.Value.ProductsCollectionName);
            usersCollection = mongoDatabase.GetCollection<dtoUserEntity>(dbSettings.Value.UsersCollectionName);
            cartCollection = mongoDatabase.GetCollection<dtoCartEntity>(dbSettings.Value.CartCollectionName);
            ordersCollection = mongoDatabase.GetCollection<dtoOrderEntity>(dbSettings.Value.OrdersCollectionName);
        }

        public async Task<dtoListResponse<dtoOrderListItem>> GetOrders(dtoPaginationFilter filter, string userId)
        {
            dtoListResponse<dtoOrderListItem> response = new dtoListResponse<dtoOrderListItem>();

            try
            {
                var ordersQuery = ordersCollection
                    .Find(Builders<dtoOrderEntity>.Filter.Eq(o => o.userId, userId))
                    .SortByDescending(o => o.orderDate);

                long resultsCount = await ordersQuery.CountDocumentsAsync();

                List<dtoOrderEntity> data = await ordersQuery
                    .Skip(filter.pageIndex * filter.pageSize)
                    .Limit(filter.pageSize)
                    .ToListAsync();

                foreach (dtoOrderEntity order in data)
                {
                    dtoOrderProducts orderProducts = await this.GetProductsFromOrder(order.products, 3);
                    order.order = orderProducts;
                }

                response.data = data.Cast<dtoOrderListItem>().ToList();
                response.resultsCount = resultsCount;
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "We couldn't get the list of orders";
            }

            return response;
        }

        public async Task<dtoActionResponse<dtoOrderListItem>> GetOrderDetail(string orderId, string userId)
        {
            dtoActionResponse<dtoOrderListItem> response = new dtoActionResponse<dtoOrderListItem>();

            try
            {
                dtoOrderEntity? data = await ordersCollection
                    .Find(Builders<dtoOrderEntity>.Filter.Eq(p => p.orderId, orderId)
                        & Builders<dtoOrderEntity>.Filter.Eq(p => p.userId, userId))
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    response.error = dtoResponseMessageCodes.NOT_EXISTS;
                    response.message = "Selected order doesn't exist";
                    return response;
                }

                dtoOrderProducts orderProducts = await this.GetProductsFromOrder(data.products, null);
                data.order = orderProducts;

                response.data = (dtoOrderListItem)data;
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "We couldn't get the details of the order";
            }

            return response;
        }

        public async Task<dtoActionResponse<bool>> CreateOrder(List<dtoOrderProduct> products, string userId)
        {
            dtoActionResponse<bool> response = new dtoActionResponse<bool>();
            double totalPrice = 0;

            try
            {
                // Check if any product doesn't exist
                foreach (dtoOrderProduct product in products)
                {
                    dtoProductEntity? p = await productsCollection
                        .Find(p => p.productId == product.productId)
                        .FirstOrDefaultAsync();

                    if (p == null)
                    {
                        response.error = dtoResponseMessageCodes.NOT_EXISTS;
                        response.message = string.Format("Product {0} doesn't exist", product.productId);
                        break;
                    }

                    if (product.quantity < 1 || product.quantity > 30)
                    {
                        response.error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED;
                        response.message = string.Format("You can't buy {0} units of product {1}", product.quantity, p.name);
                        break;
                    }

                    totalPrice += p.price * product.quantity;
                }

                if (response.error != null)
                    return response;

                DateTime orderDate = DateTime.UtcNow;
                DateTime deliveryDate = orderDate.AddDays(new Random().Next(1, 11));

                dtoOrderEntity newOrder = new dtoOrderEntity
                {
                    userId = userId,
                    products = products,
                    orderDate = orderDate,
                    deliveryDate = deliveryDate,
                    status = OrderStatus.NOT_SHIPPED,
                    total = totalPrice
                };

                await ordersCollection.InsertOneAsync(newOrder);

                // Created
                if (newOrder.orderId != null)
                {
                    response.data = true;

                    foreach (dtoOrderProduct product in products)
                        await cartCollection.DeleteOneAsync(p => p.product.productId == product.productId && p.userId == userId);
                }
                // Not created
                else
                {
                    response.error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED;
                    response.message = "We couldn't add the product to cart";
                }
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "We couldn't add the product to cart";
            }

            return response;
        }

        public async Task<dtoActionResponse<bool>> CancelOrder(string orderId, string userId)
        {
            dtoActionResponse<bool> response = new dtoActionResponse<bool>();

            try
            {
                var deleteResult = await ordersCollection
                    .DeleteOneAsync(o => o.orderId == orderId && o.userId == userId);

                if (deleteResult.DeletedCount == 0)
                {
                    response.error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED;
                    response.message = "We couldn't cancel the order";
                    return response;
                }

                response.data = true;
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "We couldn't cancel the order";
            }

            return response;
        }

        private async Task<dtoOrderProducts> GetProductsFromOrder(List<dtoOrderProduct> products, int? limit)
        {
            try
            {
                dtoOrderProducts orderProducts = new dtoOrderProducts();

                IEnumerable<string> productIds = products.Select(p => p.productId);
                var productQuery = productsCollection
                    .Find(p => productIds.Contains(p.productId))
                    .SortByDescending(p => p.price);

                long productsCount = await productQuery.CountDocumentsAsync();

                List<dtoOrderListItemProduct> productEntities = await productQuery
                    .Limit(limit)
                    .Project(p => new dtoOrderListItemProduct
                    {
                        productId = p.productId,
                        name = p.name,
                        price = p.price,
                        category = p.category,
                        image = p.image
                    })
                    .ToListAsync();

                productEntities.ForEach(item =>
                {
                    item.quantity = products.Find(p => p.productId == item.productId)?.quantity ?? 0;
                });

                orderProducts.products = productEntities;
                orderProducts.productsCount = (int)productsCount;

                return orderProducts;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
