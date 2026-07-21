using System.ComponentModel.DataAnnotations;

namespace HCMv3.Maui.Models.Settings;

public class ChangePasswordModel
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    public string NewPassword { get; set; }

    [Required]
    [Compare(nameof(NewPassword))]
    public string NewPasswordRepeat { get; set; }
}