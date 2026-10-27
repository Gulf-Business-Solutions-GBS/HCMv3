using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditPayslipConfigDto
{
    public int? Id { get; set; }

    [Required]
    public int EmailTemplateId { get; set; }
}
