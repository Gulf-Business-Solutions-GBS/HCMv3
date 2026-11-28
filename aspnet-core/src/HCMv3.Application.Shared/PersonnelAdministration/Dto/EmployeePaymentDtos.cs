using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeePaymentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int WageTypeId { get; set; }
    public string WageTypeNameEn { get; set; }

    /// <summary>Legacy non-persistent <c>BillingType</c>: the wage type's billing type (Earning, Deduction, ...).</summary>
    public string BillingType { get; set; }

    /// <summary>Legacy display name "Number": an amount, or a count of days or hours (see <see cref="UnitType"/>).</summary>
    public double Amount { get; set; }
    public int? CurrencyId { get; set; }
    public string CurrencyNameEn { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Comments { get; set; }
    public UnitType UnitType { get; set; }
    public int? CostCenterId { get; set; }
    public string CostCenterNameEn { get; set; }

    /// <summary>Legacy non-persistent <c>ExpectedAmount</c>; 0 unless the unit type is Day or Hour.</summary>
    public double ExpectedAmount { get; set; }
}

public class CreateOrEditEmployeePaymentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    /// <summary>Legacy display name "Number".</summary>
    public double Amount { get; set; }

    /// <summary>Required (legacy validation). Left empty, the default currency is used.</summary>
    public int? CurrencyId { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public UnitType UnitType { get; set; }

    public int? CostCenterId { get; set; }
}

public class GetEmployeePaymentForEditOutput
{
    public CreateOrEditEmployeePaymentDto EmployeePayment { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    /// <summary>Shown in the wage type list even when that wage type is outside the lookup's criteria.</summary>
    public string WageTypeNameEn { get; set; }

    public string BillingType { get; set; }

    public double ExpectedAmount { get; set; }
}

/// <summary>Input of the live Expected Amount figure: the five fields legacy recalculated on.</summary>
public class GetExpectedPaymentAmountInput
{
    public int EmployeeId { get; set; }

    public int WageTypeId { get; set; }

    public DateTime PaymentDate { get; set; }

    public UnitType UnitType { get; set; }

    /// <summary>The "Number" field.</summary>
    public double Amount { get; set; }
}

public class GetEmployeePaymentEmployeesInput
{
    /// <summary>Optional text matched against the employee's code and English and Arabic names.</summary>
    public string Filter { get; set; }
}

/// <summary>A wage type a payment may be made under, with its billing type for the read-only Billing Type field.</summary>
public class EmployeePaymentWageTypeLookupDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string BillingType { get; set; }
}

/// <summary>What the Payment screen needs before a record is opened.</summary>
public class EmployeePaymentScreenSettingsDto
{
    /// <summary>Legacy <c>CurrencyService.GetDefaultCurrency</c>; a new payment starts with it.</summary>
    public int? DefaultCurrencyId { get; set; }

    /// <summary>Payroll setting "Show Billing Type In Payment Screen".</summary>
    public bool ShowBillingType { get; set; }
}

public class GetAllEmployeePaymentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    /// <summary>Optional text matched against employee code/name and wage type name.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "PaymentDate DESC";
        }
    }
}
