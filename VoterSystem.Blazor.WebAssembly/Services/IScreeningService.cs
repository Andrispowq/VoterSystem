using ELTE.Cinema.Blazor.WebAssembly.ViewModels;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public interface IScreeningService
    {
        public Task<PagedListWrapper<ScreeningViewModel>?> GetScreeningsAsync(int page, int size, int roomId, int movieId, DateTime? startsAfter, DateTime? startsBefore);
        public Task DeleteScreeningAsync(int screeningId);
        public Task CreateScreeningAsync(ScreeningViewModel screening);
        public Task<ScreeningViewModel> GeScreeningByIdAsync(int screeningId);
        public Task UpdateScreeningAsync(ScreeningViewModel screening);
    }
}
