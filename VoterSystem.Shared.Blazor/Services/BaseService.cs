using System.Net.Http.Headers;
using System.Text.Json;

namespace VoterSystem.Shared.Blazor.Services;

public abstract class BaseService(IToastService toastService)
{
    protected async Task HandleHttpError(HttpResponseMessage response)
    {
        string responseBody = await response.Content.ReadAsStringAsync();
        JsonDocument jsonDoc;
        try
        {
            jsonDoc = JsonDocument.Parse(responseBody);
        }
        catch (System.Exception exp) when (exp is JsonException or ArgumentException)
        {
            ShowErrorMessage("Unknown error occured");
            return;
        }
            
        if (jsonDoc.RootElement.TryGetProperty("detail", out var detailElement))
        {
            var errorMessage = detailElement.GetString() ?? "Unknown error occured";
            ShowErrorMessage(errorMessage);
        }
        else
        {
            ShowErrorMessage("Unknown error occured");
        }
    }

    protected void ShowErrorMessage(string message)
    {
        toastService.ShowToast(message);
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