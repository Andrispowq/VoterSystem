namespace VoterSystem.Shared.Blazor.Services
{
    public interface IToastService
    {
        public event Action? OnToastChanged;
        public IReadOnlyList<string> Toasts { get; }
        public void ShowToast(string message);
    }
}
