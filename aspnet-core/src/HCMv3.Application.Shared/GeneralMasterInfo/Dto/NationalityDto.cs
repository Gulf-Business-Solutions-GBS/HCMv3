using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="Nationality"/>.
/// </summary>
public class NationalityDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? NationalityGroupId { get; set; }

    public string NationalityGroupName { get; set; }
}
