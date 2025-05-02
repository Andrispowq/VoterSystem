using System.Net.Http.Headers;

namespace VoterSystem.Web.Admin.ViewModels;

public class HttpResponseWrapper<T>(T response, HttpResponseHeaders headers)
{
    public T Response { get; set; } = response;
    public HttpResponseHeaders Headers { get; set; } = headers;
}