using System;
using System.Collections.Generic;
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

    public string GuarantorNameEn { get; set; }

    public string GuarantorNameAr { get; set; }

    /// <summary>The day the document was physically handed over. Null until somebody records it.</summary>
    public DateTime? ActualWithdrawDate { get; set; }

    /// <summary>The login of whoever recorded the hand-over.</summary>
    public string HandedOverByUserName { get; set; }

    /// <summary>The login of whoever took the document back.</summary>
    public string ReceivedByUserName { get; set; }

    public string ReturnRemarks { get; set; }

    /// <summary>In effect, not returned, and due back before today.</summary>
    public bool IsOverdue { get; set; }

    /// <summary>Whole days past the due date. Zero when not overdue.</summary>
    public int DaysOverdue { get; set; }

    /// <summary>The movement this one extended, when it replaced an earlier one for the same document.</summary>
    public int? ExtendsMovementId { get; set; }

    /// <summary>
    /// The earlier movements this one replaced, oldest first. Empty for a withdrawal that was never extended.
    /// </summary>
    public List<DocumentMovementExtensionDto> Extensions { get; set; } = new List<DocumentMovementExtensionDto>();
}

/// <summary>
/// One earlier movement that an extension replaced: the period the document was first out for, before the
/// due date was moved.
/// </summary>
public class DocumentMovementExtensionDto
{
    /// <summary>Id of the replaced movement row.</summary>
    public int Id { get; set; }

    public DateTime WithdrawDate { get; set; }

    /// <summary>The date the document was due back before the extension.</summary>
    public DateTime ReturnDate { get; set; }

    /// <summary>The request that created the replaced movement. Null when HR recorded it directly.</summary>
    public string RequestNumber { get; set; }

    /// <summary>When the extension that replaced it was approved.</summary>
    public DateTime? SupersededOn { get; set; }
}

/// <summary>The day a document was physically handed to the employee.</summary>
public class RecordDocumentHandOverInput : EntityDto<int>
{
    /// <summary>Required, and not in the future. Recording it again corrects the date.</summary>
    public DateTime ActualWithdrawDate { get; set; }
}

/// <summary>HR taking a document back at the counter.</summary>
public class RecordDocumentReturnInput : EntityDto<int>
{
    /// <summary>Required, not in the future, and not before the day the document was handed over.</summary>
    public DateTime ActualReturnDate { get; set; }

    [StringLength(500)]
    public string ReturnRemarks { get; set; }
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

public class GetEmployeeDocumentMvmtForEditOutput
{
    public CreateOrEditEmployeeDocumentMvmtDto EmployeeDocumentMvmt { get; set; }

    /// <summary>
    /// The same movement as the list shows it: the read-only facts the form displays but does not edit --
    /// names, the custody trail, whether it is overdue, and what it extended.
    /// </summary>
    public EmployeeDocumentMvmtDto Details { get; set; }
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
