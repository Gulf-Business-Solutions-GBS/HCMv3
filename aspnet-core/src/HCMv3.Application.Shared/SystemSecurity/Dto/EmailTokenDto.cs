using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class EmailTokenDto : EntityDto<int>
{
    public string TokenName { get; set; }
    public string TokenValue { get; set; }
    public bool IsDateTimeType { get; set; }
    public int EmailTemplateId { get; set; }
    public string EmailTemplateNameEn { get; set; }
    public bool ObjectKey { get; set; }
}
