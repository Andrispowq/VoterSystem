using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Blazor.ViewModels;

public sealed class CreateGroupViewModel
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;
}