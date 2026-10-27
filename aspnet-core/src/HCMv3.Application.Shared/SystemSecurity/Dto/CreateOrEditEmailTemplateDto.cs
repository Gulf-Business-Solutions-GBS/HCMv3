using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditEmailTemplateDto
{
    public int? Id { get; set; }

    [StringLength(64)]
    public string Code { get; set; }

    [Required]
    [StringLength(256)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string NameAr { get; set; }

    [StringLength(256)]
    public string SmtpProfile { get; set; }

    [StringLength(500)]
    public string Subject { get; set; }

    public string EmailBodyHtml { get; set; }

    public string EmailBodyText { get; set; }
}
