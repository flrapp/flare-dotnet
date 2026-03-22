using System;
using System.Net;

namespace Flare.HttpClient;

public class FlareApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public FlareApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public FlareApiException(HttpStatusCode statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
