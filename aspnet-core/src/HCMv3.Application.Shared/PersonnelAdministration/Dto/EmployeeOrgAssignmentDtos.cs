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
