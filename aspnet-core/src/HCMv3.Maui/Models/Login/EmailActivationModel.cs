using System.ComponentModel.DataAnnotations;

namespace HCMv3.Maui.Models.Login;

public class EmailActivationModel
{
    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; }
}