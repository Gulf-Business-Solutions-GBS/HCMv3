using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDocumentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int DocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; }
    public string DocumentNum { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Comments { get; set; }
    public string Description { get; set; }

    /// <summary>Computed (not stored) -- see <c>EmployeeDocumentsAppService.ComputeIsActive</c>.</summary>
    public DocumentStatus IsActive { get; set; }

    /// <summary>Computed (not stored) -- IN unless a child movement lacks an ActualReturnDate.</summary>
    public DocumentMvmtStatus DocumentMvmtStatus { get; set; }

    /// <summary>Computed (not stored) -- days between today and ExpiryDate.</summary>
    public double DaysLeft { get; set; }
}

public class CreateOrEditEmployeeDocumentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int DocumentTypeId { get; set; }

    [StringLength(128)]
    public string DocumentNum { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    /// <summary>
    /// Record this as final instead of sending it for approval.
    ///
    /// <para>
    /// Offered only to a user holding <c>RecordWithoutApproval</c>, and only on a type whose catalog row
    /// permits a bypass. The server checks both and refuses otherwise, so this being true is a request
    /// rather than a decision.
    /// </para>
    /// </summary>
    public bool RecordDirectly { get; set; }
}

public class GetEmployeeDocumentForEditOutput
{
    public CreateOrEditEmployeeDocumentDto EmployeeDocument { get; set; }
}

public class GetAllEmployeeDocumentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "IssueDate DESC";
        }
    }
}
