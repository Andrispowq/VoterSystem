using ELTE.Cinema.Blazor.WebAssembly.Services;
using VoterSystem.Web.Admin.Config;
using Timer = System.Timers.Timer;

namespace VoterSystem.Web.Admin.Services;

public class ToastService(AppConfig appConfig) : IToastService
{
    public event Action? OnToastChanged;
    public IReadOnlyList<string> Toasts => _toasts;

    private readonly List<string> _toasts = new();
    
    public void ShowToast(string message)
    {
        _toasts.Insert(0, message);
        OnToastChanged?.Invoke();

        var timer = new Timer(appConfig.ToastDurationInMillis);
        timer.Elapsed += (_, _) =>
        {
            timer.Dispose();
            RemoveToast(message);
        };
        timer.Start();
    }

    private void RemoveToast(string message)
    {
        //if show more toast with same message the last is the oldest one
        int lastIndex = _toasts.LastIndexOf(message);
        if (lastIndex >= 0)
        {
            _toasts.RemoveAt(lastIndex);
            OnToastChanged?.Invoke();
        }
    }
}