using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditEmailTokenDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string TokenName { get; set; }

    [StringLength(1000)]
    public string TokenValue { get; set; }

    public bool IsDateTimeType { get; set; }

    [Required]
    public int EmailTemplateId { get; set; }

    public bool ObjectKey { get; set; }
}
