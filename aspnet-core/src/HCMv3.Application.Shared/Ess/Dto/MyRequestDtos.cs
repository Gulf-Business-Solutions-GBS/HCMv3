using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.GovtRelationsManagement;
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

public class MyAttendanceRequestDto : MyRequestDtoBase
{
    public int AttendanceTypeId { get; set; }

    public string AttendanceTypeNameEn { get; set; }

    public string AttendanceTypeNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public double AttendanceHours { get; set; }

    public string Comments { get; set; }
}

public class RequestAttendanceCorrectionInput
{
    public int AttendanceTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public double AttendanceHours { get; set; }

    /// <summary>Why the correction is needed. An approver cannot check a claim about a day that has already
    /// passed without being told what went wrong.</summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Comments { get; set; }
}

public class MyBankRequestDto : MyRequestDtoBase
{
    public int? BankId { get; set; }

    public string BankNameEn { get; set; }

    public string BankNameAr { get; set; }

    /// <summary>Masked to the last four characters. The full value is never sent to a list screen.</summary>
    public string AccountNumMasked { get; set; }

    /// <summary>Masked to the last four characters.</summary>
    public string IbanMasked { get; set; }

    public string BankBranch { get; set; }

    public DateTime StartDate { get; set; }
}

public class RequestBankChangeInput
{
    public int? BankId { get; set; }

    public string AccountNum { get; set; }

    public string IBAN { get; set; }

    public string BankBranch { get; set; }

    /// <summary>When the new details should take effect -- never before approval, whatever this says.</summary>
    public DateTime StartDate { get; set; }
}

public class MyTravelClaimDto : MyRequestDtoBase
{
    public int? TravelId { get; set; }

    public DateTime ClaimDate { get; set; }

    public double ClaimAmount { get; set; }

    public string Comments { get; set; }
}

public class MyExitPermitDto : MyRequestDtoBase
{
    /// <summary>The number on the permit the government issued.</summary>
    public string ExternalRequestNumber { get; set; }

    public TravelReason TravelReason { get; set; }

    public DateTime ExitDate { get; set; }

    public DateTime StartDate { get; set; }

    public string Comments { get; set; }
}

public class MyHRServiceRequestDto : MyRequestDtoBase
{
    public int HRServiceTypeId { get; set; }

    public string HRServiceTypeNameEn { get; set; }

    public string HRServiceTypeNameAr { get; set; }

    public DateTime ServiceDate { get; set; }

    public double Amount { get; set; }

    public string Comments { get; set; }
}

public class RequestExitPermitInput
{
    /// <summary>
    /// The government's permit number. Keyed in rather than generated: the permit is obtained outside this
    /// system and the employee already has it in hand. Required by the handler as well; validated in both
    /// places because the handler is the rule and this is the early, friendlier failure.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ExternalRequestNumber { get; set; }

    public TravelReason TravelReason { get; set; }

    /// <summary>The date the employee leaves the country, as printed on the permit.</summary>
    public DateTime ExitDate { get; set; }

    /// <summary>When the permit takes effect for the company's own records.</summary>
    public DateTime StartDate { get; set; }

    public string Comments { get; set; }
}

public class RequestHRServiceInput
{
    public int HRServiceTypeId { get; set; }

    /// <summary>When the employee needs it by. Defaults to today on the screen, as legacy did.</summary>
    public DateTime ServiceDate { get; set; }

    /// <summary>Optional -- most HR services carry no charge at all, so zero is a real answer rather than a
    /// missing one, and there is no minimum to enforce.</summary>
    [Range(0, 1_000_000_000)]
    public double Amount { get; set; }

    public string Comments { get; set; }
}

public class RequestTravelClaimInput
{
    /// <summary>The trip being claimed against. Optional, because not every claim has a recorded trip.</summary>
    public int? TravelId { get; set; }

    public DateTime ClaimDate { get; set; }

    // A finite upper bound on purpose. double.MaxValue serialises into the OpenAPI document as
    // 1.797E+308, which NSwag cannot parse as a decimal, and proxy generation fails for the whole API --
    // the rule CLAUDE.md states as "do not use double.Max as a max attribute".
    [Range(0.01, 1_000_000_000)]
    public double ClaimAmount { get; set; }

    public string Comments { get; set; }
}

/// <summary>
/// A passport (or other document) withdrawal, as the employee sees it.
///
/// <para>
/// One DTO for the request and for the movement, because there is one row. What legacy showed on the
/// <c>PassportWithdrawalRequest</c> screen and what it showed on the document movement log were the same
/// facts held in two tables; here <see cref="ActualReturnDate"/> is simply null until the document actually
/// comes back.
/// </para>
/// </summary>
public class MyPassportWithdrawalDto : MyRequestDtoBase
{
    public int EmployeeDocumentId { get; set; }

    public string DocumentNum { get; set; }

    public int? DocumentTypeId { get; set; }

    public string DocumentTypeNameEn { get; set; }

    public string DocumentTypeNameAr { get; set; }

    public DateTime WithdrawDate { get; set; }

    public DateTime ReturnDate { get; set; }

    /// <summary>Null while the document is still out; set only once it has been handed back.</summary>
    public DateTime? ActualReturnDate { get; set; }

    public int? GuarantorId { get; set; }

    public string GuarantorNameEn { get; set; }

    public string GuarantorNameAr { get; set; }

    public string WithdrawalReason { get; set; }

    public string Comments { get; set; }

    /// <summary>
    /// Whether the employee can report this document as handed back. True only once the withdrawal is
    /// approved and the document is not already recorded as returned: offering the button before approval
    /// would be offering to return a document that was never released, and offering it twice is how the
    /// custody record ends up with two different return dates for one movement.
    /// </summary>
    public bool CanReturn { get; set; }
}

public class RequestPassportWithdrawalInput
{
    /// <summary>The employee's own document row. Ownership is re-checked on the server: a document
    /// belonging to somebody else is refused rather than quietly withdrawn on their behalf.</summary>
    [Required]
    public int EmployeeDocumentId { get; set; }

    public DateTime WithdrawDate { get; set; }

    public DateTime ReturnDate { get; set; }

    /// <summary>Who vouches for the document coming back. Optional -- not every tenant asks for one.</summary>
    public int? GuarantorId { get; set; }

    /// <summary>
    /// Why the document is needed. Whether it may be left blank is a tenant setting
    /// (<c>ManageSelfServiceSettings.IsWithdrawalReasonMandatory</c>), which is why it is deliberately NOT
    /// annotated <c>[Required]</c>: an annotation cannot read a setting, and adding one would make the field
    /// mandatory for every tenant including those that never asked for it.
    /// </summary>
    [StringLength(500)]
    public string WithdrawalReason { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
