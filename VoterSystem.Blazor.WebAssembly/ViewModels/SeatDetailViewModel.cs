namespace ELTE.Cinema.Blazor.WebAssembly.ViewModels
{
    public class SeatDetailViewModel
    {
        public SeatViewModel Seat { get; set; } = null!;
        public ReservationViewModel? Reservation { get; set; }
    }
}
