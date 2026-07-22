using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Shared read/list DTO reused by every pure bilingual lookup app service
/// (Country, Title, Religion, etc. — anything deriving from <see cref="SimpleLookupEntity"/>).
/// </summary>
public class SimpleLookupDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
