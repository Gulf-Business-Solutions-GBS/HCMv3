using System.ComponentModel.DataAnnotations;

namespace HCMv3.Integrations.Dto;

public class SetIntegrationEnabledInput
{
    [Required]
    [StringLength(64)]
    public string Key { get; set; }

    public bool Enabled { get; set; }
}
