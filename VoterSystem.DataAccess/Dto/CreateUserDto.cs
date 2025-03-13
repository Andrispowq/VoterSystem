using System.ComponentModel.DataAnnotations;
using VoterSystem.DataAccess.Model;

namespace VoterSystem.DataAccess.Dto;

public class CreateUserDto
{
    [MaxLength(50)] public required string Name { get; set; }
    [MaxLength(50)] public required string Email { get; set; }
    [MaxLength(50)] public required string Password { get; set; }
    public required UserLevel UserLevel { get; init; }
}