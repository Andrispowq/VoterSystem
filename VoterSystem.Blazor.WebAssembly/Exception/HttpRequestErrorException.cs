namespace ELTE.Cinema.Blazor.WebAssembly.Exception
{
    public class HttpRequestErrorException(HttpResponseMessage response)
        : System.Exception($"HTTP request failed with status code {response.StatusCode}")
    {
        public HttpResponseMessage Response { get; } = response ?? throw new ArgumentNullException(nameof(response));
    }
}
