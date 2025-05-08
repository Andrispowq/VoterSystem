using System.ComponentModel.DataAnnotations;

namespace VoterSystem.Shared.Dto;

public class VotingRequestDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255)]
    public required string Name { get; set; }
    
    [Required(ErrorMessage = "StartsAt is required")]
    public required DateTime StartsAt { get; set; }
    
    [Required(ErrorMessage = "StartsAt is required")]
    public required DateTime EndsAt { get; set; }
}