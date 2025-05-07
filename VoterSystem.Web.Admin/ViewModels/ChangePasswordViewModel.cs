namespace VoterSystem.Web.Admin.ViewModels;

using System.ComponentModel.DataAnnotations;

public class ChangePasswordViewModel : IValidatableObject
{
    [Required]
    [DataType(DataType.Password)]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OldPassword == NewPassword)
        {
            yield return new ValidationResult(
                "New password must be different from the current password.",
                [nameof(NewPassword)]);
        }
    }
}