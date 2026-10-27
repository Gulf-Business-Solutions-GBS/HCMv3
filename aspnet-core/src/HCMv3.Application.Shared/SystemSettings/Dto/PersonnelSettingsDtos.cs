using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class PersonnelSettingsListDto : EntityDto
{
    public bool EnableSponsorGovtJobTitleQuota { get; set; }

    public int MaxPhotoSize { get; set; }

    public ProbationDaysCalculation ProbationDaysCalculation { get; set; }

    public bool AutoWageAssignOnHire { get; set; }
}

public class PersonnelSettingsEditDto : EntityDto
{
    public bool EnableSponsorGovtJobTitleQuota { get; set; }

    public int MaxPhotoSize { get; set; }

    public ProbationDaysCalculation ProbationDaysCalculation { get; set; }

    public bool AutoWageAssignOnHire { get; set; }
}
