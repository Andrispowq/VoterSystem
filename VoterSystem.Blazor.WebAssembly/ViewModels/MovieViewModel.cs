using System.ComponentModel.DataAnnotations;

namespace ELTE.Cinema.Blazor.WebAssembly.ViewModels
{
    public class MovieViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title shouldn't be empty")]
        [MaxLength(255, ErrorMessage = "Title is too long")]
        public string? Title { get; set; }

        [Range(1000, int.MaxValue, ErrorMessage = "Year should be greater than 1000")]
        public int Year { get; set; }

        [MaxLength(255, ErrorMessage = "Director name is too long")]
        public string? Director { get; set; }

        public string? Synopsis { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Length should be greater than 1")]
        public int Length { get; set; }

        [Required(ErrorMessage = "Uploading an image is required")]
        public byte[]? Image { get; set; }
    }
}
