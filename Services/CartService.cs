using MongoDB.Driver;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;
using PureFashion.Models.Product;
using PureFashion.Models.User;
using PureFashion.Models.Cart;

namespace PureFashion.Services.Cart
{
    public class CartService : ICartService
    {
        private readonly IConfiguration _configuration;

        private readonly IMongoCollection<dtoProductEntity> productsCollection;
        private readonly IMongoCollection<dtoUserEntity> usersCollection;
        private readonly IMongoCollection<dtoCartEntity> cartCollection;

        private readonly IOptions<PureFashionDatabaseSettings> _dbSettings;

        public CartService(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            _configuration = configuration;
            _dbSettings = dbSettings;

            MongoClient mongoClient = new MongoClient(dbSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            productsCollection = mongoDatabase.GetCollection<dtoProductEntity>(dbSettings.Value.ProductsCollectionName);
            usersCollection = mongoDatabase.GetCollection<dtoUserEntity>(dbSettings.Value.UsersCollectionName);
            cartCollection = mongoDatabase.GetCollection<dtoCartEntity>(dbSettings.Value.CartCollectionName);
        }

        public async Task<dtoListResponse<dtoProductCartData>> GetProductsFromCart(dtoPaginationFilter filter, string userId)
        {
            dtoListResponse<dtoProductCartData> response = new dtoListResponse<dtoProductCartData>();

            try
            {
                var matchFilter = Builders<dtoCartEntity>.Filter.Eq(p => p.userId, userId);

                var cartQuery = cartCollection
                    .Find(matchFilter)
                    .SortByDescending(item => item.addedDate);

                long resultsCount = await cartQuery.CountDocumentsAsync();

                var products = await cartQuery
                    .Skip(filter.pageIndex * filter.pageSize)
                    .Limit(filter.pageSize)
                    .Project(p => new dtoProductCartData
                    {
                        productId = p.product.productId,
                        name = p.product.name,
                        image = p.product.image,
                        category = p.product.category,
                        price = p.product.price,
                        addedDate = p.addedDate
                    })
                    .ToListAsync();

                response.data = products;
                response.resultsCount = resultsCount;
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "There was an error fetching the cart data";
            }

            return response;
        }

        public async Task<dtoActionResponse<bool>> AddProductToCart(int productId, string userId)
        {
            dtoActionResponse<bool> response = new dtoActionResponse<bool>();

            try
            {
                dtoProductEntity? product = await productsCollection
                    .Find(p => p.productId == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.error = dtoResponseMessageCodes.NOT_EXISTS;
                    response.message = "This product doesn't exist";
                    return response;
                }

                dtoCartEntity? dbCart = await cartCollection
                    .Find(Builders<dtoCartEntity>.Filter.Eq(c => c.product.productId, productId)
                    & Builders<dtoCartEntity>.Filter.Eq(c => c.userId, userId))
                    .FirstOrDefaultAsync();

                if (dbCart != null)
                {
                    response.error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED;
                    response.message = "You already have the product added to your cart";
                    return response;
                }

                dtoCartEntity addToCart = new dtoCartEntity
                {
                    product = new dtoProductData
                    {
                        productId = product.productId,
                        name = product.name,
                        price = product.price,
                        category = product.category,
                        image = product.image
                    },
                    userId = userId,
                    addedDate = DateTime.UtcNow
                };

                await this.cartCollection.InsertOneAsync(addToCart);

                if (addToCart.id != null)
                    response.data = true;
                else
                {
                    response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
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

        public async Task<dtoActionResponse<bool>> RemoveProductFromCart(int productId, string userId)
        {
            dtoActionResponse<bool> response = new dtoActionResponse<bool>();

            try
            {
                dtoProductEntity? product = await productsCollection
                    .Find(p => p.productId == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.error = dtoResponseMessageCodes.NOT_EXISTS;
                    response.message = "This product doesn't exist";
                    return response;
                }

                DeleteResult deleteResult = await cartCollection
                    .DeleteOneAsync(c => c.product.productId == productId && c.userId == c.userId);

                if (deleteResult.DeletedCount == 0)
                {
                    response.error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED;
                    response.message = "We couldn't remove the product from cart";
                    return response;
                }

                response.data = true;
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "We couldn't remove the product to cart";
            }

            return response;
        }
    }
}
