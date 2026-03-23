using System.Net;

namespace OpenAiServiceClients.Core;

public sealed class GatewayApiException : Exception
{
    public GatewayApiException(HttpStatusCode statusCode, string message, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }
}
