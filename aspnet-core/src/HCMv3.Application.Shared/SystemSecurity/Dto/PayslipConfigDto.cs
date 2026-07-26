using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class PayslipConfigDto : EntityDto<int>
{
    public int EmailTemplateId { get; set; }
    public string EmailTemplateNameEn { get; set; }
}
