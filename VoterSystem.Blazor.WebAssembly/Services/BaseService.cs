using System.Net.Http.Headers;
using System.Text.Json;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public abstract class BaseService
    {
        private readonly IToastService _toastService;

        protected BaseService(IToastService toastService)
        {
            _toastService = toastService;
        }

        protected async Task HandleHttpError(HttpResponseMessage response)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(responseBody);
            }
            catch (System.Exception exp) when (exp is JsonException || exp is ArgumentException)
            {
                ShowErrorMessage("Unknown error occured");
                return;
            }
            
            if (jsonDoc.RootElement.TryGetProperty("detail", out JsonElement detailElement))
            {
                string errorMessage = (detailElement.GetString()) ?? "Unknown error occured";
                ShowErrorMessage(errorMessage);
            }
            else
            {
                ShowErrorMessage("Unknown error occured");
            }
        }

        protected void ShowErrorMessage(string message)
        {
            _toastService.ShowToast(message);
        }

        protected int GetPagedListTotalCount(HttpResponseHeaders headers)
        {
            if (headers.TryGetValues("X-Count", out var values) && int.TryParse(values.FirstOrDefault(), out int count))
            {
                return count;
            }
            throw new ArgumentException("No totalCount number found.");
        }
    }
}
