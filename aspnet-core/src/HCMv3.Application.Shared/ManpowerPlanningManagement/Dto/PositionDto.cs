using System;
using Abp.Application.Services.Dto;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Position"/>.
/// </summary>
public class PositionDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string CodeWithNameEn => (Code ?? "") + "-" + (NameEn ?? "");

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool Vacant { get; set; }

    public bool Active { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }

    public int? EmployeeGroupId { get; set; }

    public string EmployeeGroupNameEn { get; set; }

    public int? EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupNameEn { get; set; }

    public int? PayrollAreaId { get; set; }

    public string PayrollAreaName { get; set; }

    public int? WorkLocationId { get; set; }

    public string WorkLocationNameEn { get; set; }

    public int? CostCenterId { get; set; }

    public string CostCenterNameEn { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string OrganizationUnitDisplayName { get; set; }

    public double MinSalary { get; set; }

    public double MidSalary { get; set; }

    public double MaxSalary { get; set; }

    public bool IsBudgeted { get; set; }

    public int FTELimit { get; set; }
}
