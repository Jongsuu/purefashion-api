using MongoDB.Driver;
using Microsoft.Extensions.Options;
using PureFashion.Models.DatabaseSettings;
using PureFashion.Models.Response;
using PureFashion.Models.Product;
using PureFashion.Models.User;
using PureFashion.Models.Review;
using PureFashion.Models.Cart;
using System.Text.Json;

namespace PureFashion.Services.Product
{
    public class ProductService : IProductService
    {
        private readonly IConfiguration _configuration;

        private readonly IMongoCollection<dtoProductEntity> productsCollection;
        private readonly IMongoCollection<dtoUserEntity> usersCollection;
        private readonly IMongoCollection<dtoReviewEntity> reviewCollection;
        private readonly IMongoCollection<dtoCartEntity> cartCollection;

        private readonly IOptions<PureFashionDatabaseSettings> _dbSettings;

        public ProductService(IConfiguration configuration, IOptions<PureFashionDatabaseSettings> dbSettings)
        {
            _configuration = configuration;
            _dbSettings = dbSettings;

            MongoClient mongoClient = new MongoClient(dbSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            productsCollection = mongoDatabase.GetCollection<dtoProductEntity>(dbSettings.Value.ProductsCollectionName);
            usersCollection = mongoDatabase.GetCollection<dtoUserEntity>(dbSettings.Value.UsersCollectionName);
            reviewCollection = mongoDatabase.GetCollection<dtoReviewEntity>(dbSettings.Value.ReviewsCollectionName);
            cartCollection = mongoDatabase.GetCollection<dtoCartEntity>(dbSettings.Value.CartCollectionName);
        }

        public async Task<dtoListResponse<dtoProductListItem>> GetAllProducts(dtoProductListFilter filter)
        {
            dtoListResponse<dtoProductListItem> response = new dtoListResponse<dtoProductListItem>();

            try
            {
                var projection = Builders<dtoProductEntity>.Projection
                    .Include(d => d.productId)
                    .Include(d => d.name)
                    .Include(d => d.image)
                    .Include(d => d.price)
                    .Include(d => d.category);

                FilterDefinition<dtoProductEntity> matchFilter;

                if (filter.category != null)
                    matchFilter = Builders<dtoProductEntity>.Filter.Eq(p => p.category, filter.category.ToString());
                else
                    matchFilter = Builders<dtoProductEntity>.Filter.Empty;

                var productsQuery = productsCollection
                    .Find(matchFilter)
                    .Project<dtoProductEntity>(projection)
                    .SortBy(item => item.productId);

                long resultsCount = await productsQuery.CountDocumentsAsync();

                var products = await productsQuery
                    .Skip(filter.pageIndex * filter.pageSize)
                    .Limit(filter.pageSize)
                    .ToListAsync();

                response.data = products.Cast<dtoProductListItem>().ToList();
                response.resultsCount = resultsCount;

                foreach (dtoProductListItem item in response.data)
                {
                    await this.GetProductReviewData(item);
                }
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "There was an error fetching the products";
            }

            return response;
        }

        public async Task<dtoActionResponse<dtoProductEntity?>> GetProductDetail(int productId, string? userId)
        {
            dtoActionResponse<dtoProductEntity?> response = new dtoActionResponse<dtoProductEntity?>();

            try
            {
                var filter = PipelineStageDefinitionBuilder.Match(Builders<dtoProductEntity>.Filter.Eq(p => p.productId, productId));
                var authorLookup = PipelineStageDefinitionBuilder.Lookup<dtoProductEntity, dtoUserEntity, dtoProductEntity>(
                                usersCollection,
                                p => p.authorId,
                                u => u.id,
                                p => p.author);

                var unwind = PipelineStageDefinitionBuilder.Unwind<dtoProductEntity, dtoProductEntity>(p => p.author);

                var reviewLookup = PipelineStageDefinitionBuilder.Lookup<dtoProductEntity, dtoReviewEntity, dtoProductEntity>(
                                reviewCollection,
                                p => p.productId,
                                r => r.productId,
                                p => p.reviews);

                var projection = PipelineStageDefinitionBuilder.Project<dtoProductEntity, dtoProductEntity>(
                        Builders<dtoProductEntity>.Projection.Expression(p => new dtoProductEntity
                        {
                            productId = productId,
                            name = p.name,
                            description = p.description,
                            price = p.price,
                            image = p.image,
                            author = new dtoAuthor
                            {
                                username = p.author!.username
                            }
                        }));

                var pipeline = PipelineDefinition<dtoProductEntity, dtoProductEntity>.Create(new[] { filter, authorLookup, unwind, projection });

                dtoProductEntity? data = (await productsCollection
                    .AggregateAsync(pipeline))
                    .FirstOrDefault();

                if (data != null)
                {
                    data.inCart = await this.IsProductInCart(productId, userId);
                    await this.GetProductReviews(data);
                    response.data = (dtoProductEntity)data;
                }
                else
                {
                    response.error = dtoResponseMessageCodes.NOT_EXISTS;
                    response.message = "Selected product doesn't exist";
                }
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "There was an error fetching the product";
            }

            return response;
        }

        public async Task<dtoActionResponse<bool>> CreateProduct(dtoProductItem newProduct, string authorId)
        {
            dtoActionResponse<bool> response = new dtoActionResponse<bool>();

            try
            {
                dtoProductEntity newItem = (dtoProductEntity)newProduct;
                newItem.authorId = authorId;
                dtoProductEntity? last = await productsCollection
                    .Find(Builders<dtoProductEntity>.Filter.Empty)
                    .SortByDescending(item => item.productId)
                    .FirstOrDefaultAsync();
                newItem.productId = last != null ? last.productId + 1 : 1;
                await productsCollection.InsertOneAsync(newItem);

                if (newItem.id == null)
                {
                    response.data = false;
                    response.error = dtoResponseMessageCodes.OPERATION_NOT_PERFORMED;
                    response.message = "Product couldn't be stored";
                }
                else
                    response.data = true;
            }
            catch (Exception)
            {
                response.error = dtoResponseMessageCodes.DATABASE_OPERATION;
                response.message = "There was an error adding the product";
            }

            return response;
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

        private async Task<bool> IsProductInCart(int productId, string? userId)
        {
            if (userId == null)
                return false;

            try
            {
                dtoCartEntity? cartItem = await cartCollection
                    .Find(c => c.product.productId == productId && c.userId == userId)
                    .FirstOrDefaultAsync();
                return cartItem != null;
            }
            catch (Exception)
            {

            }

            return false;
        }

        private async Task GetProductReviewData(dtoProductListItem product)
        {
            try
            {
                var pipeline = new EmptyPipelineDefinition<dtoReviewEntity>()
                    .Match(Builders<dtoReviewEntity>.Filter.Eq(r => r.productId, product.productId))
                    .Group(r => r.productId, r => new dtoReviewTotal
                    {
                        reviewAverage = r.Average(f => f.rating),
                        reviewCount = r.Count()
                    });

                var productReviewData = (await reviewCollection.AggregateAsync(pipeline)).FirstOrDefault();

                if (productReviewData != null)
                {
                    product.reviewCount = productReviewData.reviewCount;
                    product.reviewAverage = productReviewData.reviewAverage;
                }
            }
            catch (Exception) { }
        }

        private async Task GetProductReviews(dtoProductEntity product)
        {
            try
            {
                // var pipeline = new EmptyPipelineDefinition<dtoReviewEntity>()
                var productReviewData = await reviewCollection
                .Aggregate()
                .Match(Builders<dtoReviewEntity>.Filter.Eq(r => r.productId, product.productId))
                .Lookup<dtoReviewEntity, dtoUserEntity, dtoReviewEntity>(
                        usersCollection,
                        r => r.userId,
                        u => u.id,
                        r => r.author)
                .Unwind<dtoReviewEntity, dtoReviewEntity>(r => r.author)
                .Project(r => new dtoReviewEntity
                {
                    productId = r.productId,
                    rating = r.rating,
                    description = r.description,
                    title = r.title,
                    author = new dtoAuthor
                    {
                        id = r.author!.id,
                        username = r.author!.username
                    }
                })
                .Group(r => r.productId, r => new dtoProductEntity
                {
                    reviewAverage = r.Average(f => f.rating),
                    reviewCount = r.Count(),
                    reviews = r.ToList()
                })
                .FirstOrDefaultAsync();

                if (productReviewData != null)
                {
                    product.reviewCount = productReviewData.reviewCount;
                    product.reviewAverage = productReviewData.reviewAverage;
                    product.reviews = productReviewData.reviews;
                }
            }
            catch (Exception) { }
        }
    }
}
