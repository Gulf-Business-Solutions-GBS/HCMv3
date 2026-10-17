using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeOrgAssignmentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? JobTitleId { get; set; }
    public string JobTitleName { get; set; }
    public int? CostCenterId { get; set; }
    public string CostCenterName { get; set; }
    public int? WorkLocationId { get; set; }
    public string WorkLocationName { get; set; }
    public int? PostingGroupId { get; set; }
    public string PostingGroupName { get; set; }
    public int? BudgetingGroupId { get; set; }
    public string BudgetingGroupName { get; set; }
    public long? OrganizationUnitId { get; set; }
    public string OrganizationUnitName { get; set; }
    public int? PositionId { get; set; }
    public string PositionName { get; set; }

    public int? EmployeeGroupId { get; set; }
    public string EmployeeGroupName { get; set; }
    public int? EmployeeSubGroupId { get; set; }
    public string EmployeeSubGroupName { get; set; }
    public int? PayrollAreaId { get; set; }
    public string PayrollAreaName { get; set; }
    public int? WorkflowGroupId { get; set; }
    public string WorkflowGroupName { get; set; }
}

public class CreateOrEditEmployeeOrgAssignmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public int? JobTitleId { get; set; }

    public int? CostCenterId { get; set; }

    public int? WorkLocationId { get; set; }

    public int? PostingGroupId { get; set; }

    public int? BudgetingGroupId { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>
    /// Legacy <c>EmployeeOrgAssignment.Position</c>; required when <c>ManageOrgSettings.EnablePosition</c> is on.
    /// Choosing a position fills the job title, groups, payroll area, work location, cost centre and org unit
    /// from it, as legacy did.
    /// </summary>
    public int? PositionId { get; set; }

    public int? EmployeeGroupId { get; set; }

    public int? EmployeeSubGroupId { get; set; }

    public int? PayrollAreaId { get; set; }

    public int? WorkflowGroupId { get; set; }
}

public class GetEmployeeOrgAssignmentForEditOutput
{
    public CreateOrEditEmployeeOrgAssignmentDto EmployeeOrgAssignment { get; set; }
}

public class GetAllEmployeeOrgAssignmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}

public class GetPositionsForOrgAssignmentInput
{
    /// <summary>The position the assignment being edited already holds, listed even though it is not vacant.</summary>
    public int? CurrentPositionId { get; set; }
}

/// <summary>A position that can be chosen on an org assignment, with the values it fills in.</summary>
public class PositionForOrgAssignmentDto : EntityDto<int>
{
    public string Code { get; set; }
    public string NameEn { get; set; }
    public string NameAr { get; set; }
    public int? JobTitleId { get; set; }
    public int? EmployeeGroupId { get; set; }
    public int? EmployeeSubGroupId { get; set; }
    public int? PayrollAreaId { get; set; }
    public int? WorkLocationId { get; set; }
    public int? CostCenterId { get; set; }
    public long? OrganizationUnitId { get; set; }
}
