using System.Text.Json.Serialization;

namespace PureFashion.Models.Response
{
    public class dtoListResponse<T> : dtoResponseError
    {
        public List<T> data { get; set; } = new List<T>();
        public long resultsCount { get; set; }
    }

    public class dtoActionResponse<T> : dtoResponseError
    {
        public T? data { get; set; }
    }

    public class dtoResponseError
    {
        public dtoResponseMessageCodes? error { get; set; }
        public string? message { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum dtoResponseMessageCodes
    {
        DATABASE_OPERATION,
        USER_EXISTS,
        WRONG_PASSWORD,
        NOT_EXISTS,
        OPERATION_NOT_PERFORMED
    }

    public class dtoPaginationFilter
    {
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
    }

    public class dtoListFilter : dtoPaginationFilter
    {
        public string? sortField { get; set; }
        public dtoListFilterSort? sortOrder { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum dtoListFilterSort
    {
        asc,
        desc
    }
}
