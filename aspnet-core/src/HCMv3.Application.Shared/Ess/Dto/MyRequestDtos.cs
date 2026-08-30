using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.Ess.Dto;

/// <summary>
/// What happened when an employee raised a request, whatever the type.
///
/// <para>
/// The two outcomes are genuinely different and the screen has to say which occurred. With workflow on, the
/// row is invisible until somebody approves it; with workflow off for that type, it is effective the moment
/// it is saved. A generic "saved" would imply an approval that may never come.
/// </para>
/// </summary>
public class MyRequestSubmissionDto
{
    /// <summary>Id of the transaction row itself -- the leave, asset, loan and so on.</summary>
    public int TransactionId { get; set; }

    /// <summary>Null when the type has workflow switched off, because no request was raised.</summary>
    public int? WorkflowRequestId { get; set; }

    public bool SubmittedForApproval { get; set; }
}

/// <summary>
/// The fields every self-service transaction list shows, regardless of type.
///
/// <para>
/// Concrete DTOs add what is specific to their transaction. Keeping the workflow fields here means a new
/// type inherits the status badge, the history button and the withdraw rule rather than reimplementing all
/// three slightly differently.
/// </para>
/// </summary>
public abstract class MyRequestDtoBase : EntityDto<int>
{
    /// <summary>Null when the row was entered by HR rather than requested, so the screen can show it as
    /// simply recorded instead of giving it a status that implies it went through approval.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }

    /// <summary>Whether the employee may still withdraw it -- false once approved or otherwise decided.</summary>
    public bool CanCancel { get; set; }
}

public class GetMyRequestsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Include rows that are finished and in the past. Off by default, because the list exists to
    /// answer "where is my request", not to be an archive.</summary>
    public bool IncludeHistory { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }

        if (MaxResultCount <= 0)
        {
            MaxResultCount = 20;
        }
    }
}

public class MyAssetRequestDto : MyRequestDtoBase
{
    public int AssetId { get; set; }

    public string AssetNameEn { get; set; }

    public string AssetNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}

public class RequestAssetInput
{
    public int AssetId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}

public class MyDocumentRequestDto : MyRequestDtoBase
{
    public int DocumentTypeId { get; set; }

    public string DocumentTypeNameEn { get; set; }

    public string DocumentTypeNameAr { get; set; }

    public string DocumentNum { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string Comments { get; set; }
}

public class RequestDocumentInput
{
    public int DocumentTypeId { get; set; }

    /// <summary>Required by the handler as well; validated in both places because the handler is the rule
    /// and this is the early, friendlier failure.</summary>
    public string DocumentNum { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string Comments { get; set; }
}

public class MyLoanRequestDto : MyRequestDtoBase
{
    public int LoanTypeId { get; set; }

    public string LoanTypeNameEn { get; set; }

    public string LoanTypeNameAr { get; set; }

    public double ActualAmount { get; set; }

    public double FixedAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime RecoveryStartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}

public class RequestLoanInput
{
    public int LoanTypeId { get; set; }

    public double ActualAmount { get; set; }

    /// <summary>The instalment. Zero means "decide it on approval", which is why it is not required here.</summary>
    public double FixedAmount { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>When deductions begin. Cannot precede the loan start date.</summary>
    public DateTime RecoveryStartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}

public class MyOvertimeRequestDto : MyRequestDtoBase
{
    public int OvertimeTypeId { get; set; }

    public string OvertimeTypeNameEn { get; set; }

    public string OvertimeTypeNameAr { get; set; }

    public DateTime EffectiveDate { get; set; }

    public double OvertimeHours { get; set; }

    /// <summary>What the approver actually granted, which can be less than what was claimed.</summary>
    public double ApprovedOvertimeHours { get; set; }

    public string Comments { get; set; }
}

public class RequestOvertimeInput
{
    public int OvertimeTypeId { get; set; }

    public DateTime EffectiveDate { get; set; }

    public double OvertimeHours { get; set; }

    public string Comments { get; set; }
}

public class MyTicketRequestDto : MyRequestDtoBase
{
    public int TicketTypeId { get; set; }

    public string TicketTypeNameEn { get; set; }

    public string TicketTypeNameAr { get; set; }

    public DateTime TicketDate { get; set; }

    public double Amount { get; set; }

    public string Comments { get; set; }
}

public class RequestTicketInput
{
    public int TicketTypeId { get; set; }

    public DateTime TicketDate { get; set; }

    /// <summary>Left at zero when the employee is claiming the entitlement rather than a specific sum;
    /// payroll prices it. Negative is refused by the handler.</summary>
    public double Amount { get; set; }

    public string Comments { get; set; }
}
