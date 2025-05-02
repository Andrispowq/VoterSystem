using System.ComponentModel.DataAnnotations;

namespace ELTE.Cinema.Blazor.WebAssembly.ViewModels
{
    public class RoomViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title shouldn't be empty")]
        [MaxLength(255, ErrorMessage = "Name is too long")]
        public string? Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Rows should be greater than 1")]
        public int Rows { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Columns should be greater than 1")]
        public int Columns { get; set; }
    }
}
