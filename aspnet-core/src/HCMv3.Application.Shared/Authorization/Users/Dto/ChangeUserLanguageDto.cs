using System.ComponentModel.DataAnnotations;

namespace HCMv3.Authorization.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}

