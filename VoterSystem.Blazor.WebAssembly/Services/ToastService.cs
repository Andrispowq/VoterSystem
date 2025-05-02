using ELTE.Cinema.Blazor.WebAssembly.Config;
using Timer = System.Timers.Timer;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public class ToastService: IToastService
    {
        public event Action? OnToastChanged;
        public IReadOnlyList<string> Toasts => _toasts;

        private readonly AppConfig _appConfig;
        
        private readonly List<string> _toasts = new();
        

        public ToastService(AppConfig appConfig)
        {
            _appConfig = appConfig;
        }

        public void ShowToast(string message)
        {
            _toasts.Insert(0, message);
            OnToastChanged?.Invoke();

            var timer = new Timer(_appConfig.ToastDurationInMillis);
            timer.Elapsed += (s, e) =>
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
}
