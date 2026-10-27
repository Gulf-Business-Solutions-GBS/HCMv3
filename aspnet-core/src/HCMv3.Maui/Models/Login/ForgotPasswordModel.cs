using System.ComponentModel.DataAnnotations;

namespace HCMv3.Maui.Models.Login;

public class ForgotPasswordModel
{
    [EmailAddress]
    [Required]
    public string EmailAddress { get; set; }
}