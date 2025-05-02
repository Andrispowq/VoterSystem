using System.ComponentModel.DataAnnotations;

namespace ELTE.Cinema.Blazor.WebAssembly.ViewModels
{
    public class ScreeningViewModel
    {
        public int Id { get; set; }

        [CustomValidation(typeof(ScreeningViewModel), nameof(ValidateMovieId))]
        public MovieViewModel? Movie { get; set; }

        [CustomValidation(typeof(ScreeningViewModel), nameof(ValidateRoomId))]
        public RoomViewModel? Room { get; set; }

        [CustomValidation(typeof(ScreeningViewModel), nameof(ValidateFutureDate))]
        public DateTime StartsAt { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "The price must be a positive value.")]
        public decimal Price { get; set; }

        public static ValidationResult? ValidateFutureDate(DateTime date, ValidationContext context)
        {
            return date > DateTime.Now
                ? ValidationResult.Success
                : new ValidationResult("The screening date must be in the future.", new[] { nameof(ScreeningViewModel.StartsAt) });
        }
        public static ValidationResult? ValidateMovieId(MovieViewModel movie, ValidationContext context)
        {
            return (movie.Id > 0)
                ? ValidationResult.Success
                : new ValidationResult("Please select a movie.", new[] { nameof(ScreeningViewModel.Movie) });
        }

        public static ValidationResult? ValidateRoomId(RoomViewModel room, ValidationContext context)
        {
            return (room.Id > 0)
                ? ValidationResult.Success
                : new ValidationResult("Please select a room.", new[] { nameof(ScreeningViewModel.Room) });
        }

    }
}
