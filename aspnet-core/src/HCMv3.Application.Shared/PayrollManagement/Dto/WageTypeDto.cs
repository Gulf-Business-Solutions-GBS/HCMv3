using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WageType"/>.
/// </summary>
public class WageTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string ShortNameEn { get; set; }

    public string ShortNameAr { get; set; }

    public BillingType BillingType { get; set; }

    public BillingMethod BillingMethod { get; set; }

    public ProrateMethod ProrateMethod { get; set; }

    public double FixedDays { get; set; }

    public double Rate { get; set; }

    public bool Status { get; set; }

    public Category Category { get; set; }

    public int? ArrearSourceWageTypeId { get; set; }

    public string ArrearSourceWageTypeName { get; set; }

    public int PayrollOrder { get; set; }

    public bool IsSecured { get; set; }

    public bool IncludeInIndemnity { get; set; }

    public bool IncludeInPaidLeave { get; set; }

    public bool IncludeInBasicPay { get; set; }

    public bool IncludeInArrearPay { get; set; }

    public bool IncludeInCalculatedPay { get; set; }

    public bool IncludeInAdvancePay { get; set; }

    public bool IncludeInOvertimePay { get; set; }

    public bool IncludeInFIPosting { get; set; }

    public bool IncludeInCutoff { get; set; }

    public bool IncludeInDisciplinary { get; set; }

    public bool IncludeInVariablePay { get; set; }

    public bool IncludeInSocialSecurity { get; set; }

    public bool IncludeInPartialPay { get; set; }

    public bool IncludeInLateEarlyDed { get; set; }

    public bool IncludeInServiceBonus { get; set; }

    public bool IncludeInFund { get; set; }

    public bool DontImpactNetPay { get; set; }
}
