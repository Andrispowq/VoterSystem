using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Dto;

public class UserChangePasswordRequestDto(ChangePasswordViewModel model)
{
    public string OldPassword => model.OldPassword;
    public string NewPassword => model.NewPassword;
}