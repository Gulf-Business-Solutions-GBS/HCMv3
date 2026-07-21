using System.ComponentModel.DataAnnotations;

namespace HCMv3.Localization.Dto;

public class CreateOrUpdateLanguageInput
{
    [Required]
    public ApplicationLanguageEditDto Language { get; set; }
}

