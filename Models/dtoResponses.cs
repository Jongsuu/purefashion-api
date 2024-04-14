using System.Text.Json.Serialization;

namespace PureFashion.Models.Response
{
    public class dtoListResponse<T> : dtoResponseError
    {
        public List<T> data { get; set; } = new List<T>();
    }

    public class dtoActionResponse<T> : dtoResponseError
    {
        public T? data { get; set; }
    }

    public class dtoResponseError
    {
        public dtoResponseMessageCodes? error { get; set; }
        public string? message { get; set; } = string.Empty;
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
}
