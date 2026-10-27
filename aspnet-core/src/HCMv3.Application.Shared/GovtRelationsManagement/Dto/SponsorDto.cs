using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Sponsor"/>.
/// </summary>
public class SponsorDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string Email { get; set; }

    public Gender Gender { get; set; }

    public string Profession { get; set; }

    public string Building { get; set; }

    public string Street { get; set; }

    public string Area { get; set; }

    public string Block { get; set; }

    public int Telephone { get; set; }

    public int NationalityId { get; set; }

    public string NationalityName { get; set; }

    public int BusinessTypeId { get; set; }

    public string BusinessTypeName { get; set; }

    public long OrganizationUnitId { get; set; }

    public string OrganizationUnitName { get; set; }

    public long UnifiedNum { get; set; }

    public long SocialSecurityNum { get; set; }

    public long MOSALID { get; set; }

    public int GovernorateId { get; set; }

    public string GovernorateName { get; set; }

    public string BusinessOwnerNameEn { get; set; }

    public string BusinessOwnerNameAr { get; set; }

    public string Fax { get; set; }

    /// <summary>Labeled "PACI Number" in the legacy UI.</summary>
    public string POBox { get; set; }
}
