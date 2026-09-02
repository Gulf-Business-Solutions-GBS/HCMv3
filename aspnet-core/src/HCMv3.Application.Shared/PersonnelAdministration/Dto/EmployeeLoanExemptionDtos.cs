using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeLoanExemptionDto : EntityDto<int>
{
    public int EmployeeLoanId { get; set; }

    /// <summary>Denormalised from the parent loan, so the list can be scoped and read without a join.</summary>
    public int EmployeeId { get; set; }

    public DateTime ExemptionDate { get; set; }
    public string Comments { get; set; }

    /// <summary>Null when HR entered the exemption directly rather than approving a request for it, so the
    /// screen can show it as simply recorded instead of implying it went through an approval chain.</summary>
    public HCMv3.Workflow.RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}

public class CreateOrEditEmployeeLoanExemptionDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeLoanId { get; set; }

    [Required]
    public DateTime ExemptionDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeLoanExemptionForEditOutput
{
    public CreateOrEditEmployeeLoanExemptionDto EmployeeLoanExemption { get; set; }
}

public class GetAllEmployeeLoanExemptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeLoanId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ExemptionDate DESC";
        }
    }
}
