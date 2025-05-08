using ELTE.Cinema.Blazor.WebAssembly.ViewModels;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public interface IReservationService
    {
        public Task<(RoomViewModel, List<SeatViewModel>)> GetSeatsByScreeningAsync(int screeningId, int roomId);
        public Task<SeatDetailViewModel> LoadSelectedSeatDataAsync(SeatViewModel seat);
        public Task<SeatViewModel?> SellSeatAsync(int screeningId, SeatViewModel seat);
        public Task DeleteReservationAsync(int reservationId);
    }
}
