using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class EmailTemplateDto : EntityDto<int>
{
    public string Code { get; set; }
    public string NameEn { get; set; }
    public string NameAr { get; set; }
    public string SmtpProfile { get; set; }
    public string Subject { get; set; }
    public string EmailBodyHtml { get; set; }
    public string EmailBodyText { get; set; }
}
