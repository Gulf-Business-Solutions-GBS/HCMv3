using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Dashboards.Payroll.Dto;

/// <summary>Which payslips a clicked figure lists.</summary>
public enum PayrollDashboardListKind
{
    /// <summary>Every payslip of a month (the latest paid month when none is given).</summary>
    Month = 0,

    /// <summary>The latest paid month's regular payslips whose basic salary falls in a band.</summary>
    BasicBand = 1,

    /// <summary>The latest paid month's payslips of employees paid by one method.</summary>
    PaymentMethod = 2,

    /// <summary>The period's payslips of one run type.</summary>
    RunType = 3
}

public enum PayrollDashboardExportPart
{
    All = 0,
    Trend = 1,
    CostMix = 2,
    ByOrgUnit = 3,
    BasicBands = 4,
    Earnings = 5,
    Deductions = 6,
    PaymentMethods = 7,
    RunTypes = 8,
    Provisions = 9,
    Employees = 10
}

/// <summary>The parts of cost to company, in the order the cost make-up chart shows them.</summary>
public enum PayrollCostComponent
{
    Basic = 0,
    OtherEarnings = 1,
    EmployerSocialSecurity = 2,
    IndemnityProvision = 3,
    LeaveProvision = 4,
    TicketProvision = 5
}

public class PayrollDashboardInput
{
    /// <summary>First day of the period. Default: 1 January of this year.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Last day of the period. Default: today.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Only employees in this unit or below it.</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>Only this employee.</summary>
    public int? EmployeeId { get; set; }
}

public class GetPayrollEmployeesInput : PayrollDashboardInput, IPagedResultRequest
{
    public PayrollDashboardListKind Kind { get; set; }

    /// <summary>For <see cref="PayrollDashboardListKind.Month"/>: any day of the month; empty = the latest paid month.</summary>
    public DateTime? Month { get; set; }

    /// <summary>For <see cref="PayrollDashboardListKind.BasicBand"/>: the band's index.</summary>
    public int? Band { get; set; }

    /// <summary>For <see cref="PayrollDashboardListKind.PaymentMethod"/>: empty = no payment method on file.</summary>
    public PaymentType? PaymentType { get; set; }

    /// <summary>For <see cref="PayrollDashboardListKind.RunType"/>.</summary>
    public PayrollRunType? RunType { get; set; }

    public int SkipCount { get; set; }

    public int MaxResultCount { get; set; } = 10;
}

public class ExportPayrollDashboardInput : PayrollDashboardInput
{
    public PayrollDashboardExportPart Part { get; set; }

    public PayrollDashboardListKind Kind { get; set; }

    public DateTime? Month { get; set; }

    public int? Band { get; set; }

    public PaymentType? PaymentType { get; set; }

    public PayrollRunType? RunType { get; set; }
}

public class GetPayrollCostCardInput
{
    public int EmployeeId { get; set; }

    /// <summary>Any day of the month; empty = the employee's latest regular payslip up to <see cref="ToDate"/>.</summary>
    public DateTime? Month { get; set; }

    /// <summary>The dashboard's period end, so the card never reaches past what the dashboard shows.</summary>
    public DateTime? ToDate { get; set; }
}

public class PayrollDashboardDto
{
    public DashboardScopeDto Scope { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    /// <summary>The last month of the period with a regular payroll result; the "as of" month of the snapshot tiles.</summary>
    public DateTime? LatestMonth { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public PayrollKpisDto Kpis { get; set; }

    public List<PayrollTrendPointDto> Trend { get; set; } = new();

    public List<PayrollCostMixDto> CostMix { get; set; } = new();

    public List<PayrollOrgUnitBucketDto> ByOrgUnit { get; set; } = new();

    public List<PayrollBasicBandDto> BasicBands { get; set; } = new();

    public List<PayrollWageTypeAmountDto> Earnings { get; set; } = new();

    public List<PayrollWageTypeAmountDto> Deductions { get; set; } = new();

    public List<PayrollPaymentMethodBucketDto> PaymentMethods { get; set; } = new();

    public List<PayrollRunTypeBucketDto> RunTypes { get; set; } = new();

    public PayrollProvisionsDto Provisions { get; set; }

    public List<DashboardOrgUnitOptionDto> OrgUnitPath { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitOptions { get; set; } = new();
}

public class PayrollKpisDto
{
    /// <summary>Employees with a regular payslip in the latest paid month.</summary>
    public int EmployeesPaid { get; set; }

    public double Gross { get; set; }

    public double Deductions { get; set; }

    public double Net { get; set; }

    public double EmployerCost { get; set; }

    public double CostToCompany { get; set; }

    /// <summary>Cost to company ÷ payslip-months; empty when nobody was paid.</summary>
    public double? AverageMonthlyCostPerEmployee { get; set; }

    public int OffCyclePayments { get; set; }
}

public class PayrollTrendPointDto
{
    public DateTime MonthStart { get; set; }

    public int Employees { get; set; }

    public double Gross { get; set; }

    public double Deductions { get; set; }

    public double Net { get; set; }

    public double EmployerCost { get; set; }

    public double CostToCompany { get; set; }
}

public class PayrollCostMixDto
{
    public PayrollCostComponent Component { get; set; }

    public double Amount { get; set; }
}

public class PayrollOrgUnitBucketDto
{
    public long? OrganizationUnitId { get; set; }

    public string Name { get; set; }

    public bool IsDirectStaff { get; set; }

    public bool HasChildren { get; set; }

    public int Employees { get; set; }

    public double Gross { get; set; }

    public double Net { get; set; }

    public double CostToCompany { get; set; }
}

public class PayrollBasicBandDto
{
    public int Index { get; set; }

    /// <summary>Lower bound, included.</summary>
    public double From { get; set; }

    /// <summary>Upper bound, excluded; empty for the top band.</summary>
    public double? To { get; set; }

    public int Employees { get; set; }
}

public class PayrollWageTypeAmountDto
{
    public int WageTypeId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public double Amount { get; set; }

    public int Employees { get; set; }
}

public class PayrollPaymentMethodBucketDto
{
    /// <summary>Empty = no payment method on file for the month.</summary>
    public PaymentType? PaymentType { get; set; }

    public int Employees { get; set; }

    public double Net { get; set; }
}

public class PayrollRunTypeBucketDto
{
    public PayrollRunType RunType { get; set; }

    public int Payslips { get; set; }

    public int Employees { get; set; }

    public double Net { get; set; }
}

/// <summary>End-of-service, leave and ticket liability carried at the end of the latest paid month.</summary>
public class PayrollProvisionsDto
{
    public double Indemnity { get; set; }

    public double Leave { get; set; }

    public double Ticket { get; set; }

    public double Total { get; set; }
}

public class PayrollEmployeeRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime Month { get; set; }

    public PayrollRunType RunType { get; set; }

    public double Basic { get; set; }

    public double Gross { get; set; }

    public double Deductions { get; set; }

    public double Net { get; set; }

    public double EmployerCost { get; set; }

    public double CostToCompany { get; set; }

    public PaymentType? PaymentType { get; set; }
}

/// <summary>One employee's cost to company for one month, line by line (legacy's CTC dashboard).</summary>
public class PayrollCostCardDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime? JoiningDate { get; set; }

    /// <summary>Empty when the employee has no regular payslip up to the period end.</summary>
    public DateTime? Month { get; set; }

    public List<PayrollCostLineDto> Earnings { get; set; } = new();

    public List<PayrollCostLineDto> Deductions { get; set; } = new();

    /// <summary>What the company pays on top of gross: employer social security and this month's provisions.</summary>
    public List<PayrollCostLineDto> EmployerCosts { get; set; } = new();

    public double Gross { get; set; }

    public double TotalDeductions { get; set; }

    public double Net { get; set; }

    public double EmployerCost { get; set; }

    public double CostToCompany { get; set; }

    /// <summary>Cost to company × 12.</summary>
    public double AnnualCostToCompany { get; set; }

    public PayrollProvisionsDto Balances { get; set; }

    /// <summary>End-of-service days accrued, from the indemnity balance line.</summary>
    public double? IndemnityDays { get; set; }

    /// <summary>Leave days carried, from the leave balance line.</summary>
    public double? LeaveDays { get; set; }
}

public class PayrollCostLineDto
{
    public int WageTypeId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public double Amount { get; set; }
}
