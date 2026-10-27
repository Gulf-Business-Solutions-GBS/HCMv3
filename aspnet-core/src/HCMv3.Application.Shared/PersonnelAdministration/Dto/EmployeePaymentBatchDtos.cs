using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>One row on the payment batch list -- the header plus the two numbers that size the decision.</summary>
public class EmployeePaymentBatchListDto : EntityDto<int>
{
    public int WageTypeId { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeNameEn { get; set; }

    public string WageTypeNameAr { get; set; }

    public DateTime PaymentDate { get; set; }

    public UnitType UnitType { get; set; }

    public int? CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public string Comments { get; set; }

    /// <summary>How many employees the batch pays.</summary>
    public int LineCount { get; set; }

    /// <summary>The sum of the lines. On the list because a batch is only meaningful with its total.</summary>
    public double TotalAmount { get; set; }

    /// <summary>
    /// Null when the type is not workflow-enabled (the batch is immediately effective, as before workflow
    /// existed); otherwise the approval state that decides whether the payments have been created.
    /// </summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}

/// <summary>One employee's line as read back for display.</summary>
public class EmployeePaymentBatchDetailDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string EmployeeFullNameAr { get; set; }

    public double Amount { get; set; }
}

/// <summary>One employee's line as submitted.</summary>
public class CreateOrEditEmployeePaymentBatchDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// A concrete bound, NOT <c>double.MaxValue</c>: that serialises into the OpenAPI document as
    /// <c>1.797E+308</c>, which NSwag cannot parse, and the failure takes out proxy generation for the WHOLE
    /// API rather than for this one field. Negative amounts are allowed because the batch's wage type may be
    /// a Deduction.
    /// </summary>
    [Range(-1_000_000_000, 1_000_000_000)]
    public double Amount { get; set; }
}

/// <summary>
/// Create or edit input for a payment batch. Editing is only accepted while the batch has not been routed --
/// see <c>EmployeePaymentBatchesAppService.UpdateAsync</c>.
/// </summary>
public class CreateOrEditEmployeePaymentBatchDto
{
    public int? Id { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public UnitType UnitType { get; set; }

    public int? CurrencyId { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }

    /// <summary>
    /// The employees being paid. Required, no duplicates, no zero amounts -- all three enforced server-side
    /// (legacy declared the last two and never called the method that checked them).
    /// </summary>
    public List<CreateOrEditEmployeePaymentBatchDetailDto> Details { get; set; }
}

public class GetEmployeePaymentBatchForEditOutput
{
    public CreateOrEditEmployeePaymentBatchDto EmployeePaymentBatch { get; set; }

    /// <summary>The lines with their employee names filled in, so the form can render without a second call.</summary>
    public List<EmployeePaymentBatchDetailDto> Details { get; set; }

    /// <summary>
    /// False once the batch has been routed. The client uses it to render read-only; the server refuses the
    /// write regardless.
    /// </summary>
    public bool IsEditable { get; set; }
}

public class GetAllEmployeePaymentBatchesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? WageTypeId { get; set; }

    /// <summary>Batches containing this employee on one of their lines.</summary>
    public int? EmployeeId { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }

    public DateTime? FromPaymentDate { get; set; }

    public DateTime? ToPaymentDate { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "PaymentDate DESC";
        }
    }
}

/// <summary>
/// A wage type a payment batch may actually be paid under (legacy <c>PaymentRequest.PaymentWageType</c>).
/// </summary>
public class PaymentWageTypeLookupDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
