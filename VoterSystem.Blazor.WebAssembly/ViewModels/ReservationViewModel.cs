namespace ELTE.Cinema.Blazor.WebAssembly.ViewModels
{
    public class ReservationViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Phone { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? Comment { get; set; }

        public required List<SeatViewModel> Seats { get; init; }
    }
}
