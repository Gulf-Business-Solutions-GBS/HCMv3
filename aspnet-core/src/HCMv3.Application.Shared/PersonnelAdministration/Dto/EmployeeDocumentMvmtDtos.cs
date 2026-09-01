using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDocumentMvmtDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public int EmployeeDocumentId { get; set; }
    public string EmployeeDocumentNum { get; set; }
    public DateTime WithdrawDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public DateTime ActualReturnDate { get; set; }
    public int? GuarantorId { get; set; }
    public string WithdrawalReason { get; set; }
    public string Comments { get; set; }

    /// <summary>
    /// Null for a movement HR recorded directly. Non-null means this row came from a self-service
    /// withdrawal request, and anything other than Approved/Closed means the document has NOT been handed
    /// over yet -- the list must badge it rather than show it as a movement that happened.
    /// </summary>
    public RequestStatus? WorkflowStatus { get; set; }
}

public class CreateOrEditEmployeeDocumentMvmtDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeDocumentId { get; set; }

    [Required]
    public DateTime WithdrawDate { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    /// <summary>Who vouches for the document coming back. Optional -- not every tenant asks for one.</summary>
    public int? GuarantorId { get; set; }

    [StringLength(500)]
    public string WithdrawalReason { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeDocumentMvmtForEditOutput
{
    public CreateOrEditEmployeeDocumentMvmtDto EmployeeDocumentMvmt { get; set; }
}

public class GetAllEmployeeDocumentMvmtsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeDocumentId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "WithdrawDate DESC";
        }
    }
}
