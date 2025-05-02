namespace ELTE.Cinema.Blazor.WebAssembly.Config
{
    public class AppConfig
    {
        public required int MaximumFileSizeInMb { get; init; }
        public required int PageSize { get; init; }
        public required long ToastDurationInMillis { get; init; }
    }
}
