using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Position"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditPositionDto
{
    public int? Id { get; set; }

    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool Vacant { get; set; }

    public bool Active { get; set; }

    public int? JobTitleId { get; set; }

    public int? EmployeeGroupId { get; set; }

    public int? EmployeeSubGroupId { get; set; }

    public int? PayrollAreaId { get; set; }

    public int? WorkLocationId { get; set; }

    public int? CostCenterId { get; set; }

    public long? OrganizationUnitId { get; set; }

    public double MinSalary { get; set; }

    public double MidSalary { get; set; }

    public double MaxSalary { get; set; }

    public bool IsBudgeted { get; set; }

    [Range(1, int.MaxValue)]
    public int FTELimit { get; set; }

    /// <summary>
    /// The position this one reports to. Null for the top of a tree, and for every position whose parent
    /// has not been filled in yet -- the Position Org Chart simply will not show those until it is.
    /// </summary>
    public int? ParentPositionId { get; set; }
}
