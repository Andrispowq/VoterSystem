namespace ELTE.Cinema.Blazor.WebAssembly.ViewModels
{
    public class SeatViewModel
    {
        public int Id { get; init; }

        public int Row { get; init; }

        public int Column { get; init; }

        public SeatStatusViewModel Status { get; set; }

        public bool IsSelected { get; set; }
        
        public int? ReservationId { get; set; }
    }
}
