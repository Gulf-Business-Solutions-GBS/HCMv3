using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filters shared by every Payroll JV (Custom 042) report -- legacy
/// <c>uscRptPayrollJV_Custom042.ascx</c>: <c>txtMonth</c>, <c>txtYear</c>, <c>cmbRunType</c>,
/// <c>cmbPayrollArea</c> and <c>txtEmpCode</c>. Unlike Custom 019, <c>cmbPayrollArea</c> here is a real
/// payroll-area picker (legacy line 625: matched by CODE), not a hard-coded Sub-Contractors toggle. The
/// legacy <c>cmbType</c> combo (10 live formats -- see <c>BindType()</c>, line 244) is NOT modelled as a
/// field here: per-format porting decision, each format is its own application service/input/permission.
/// </summary>
public abstract class GetPayrollJVCustom042ReportInputBase : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank meant "current month".</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank meant "current year".</summary>
    public int? Year { get; set; }

    /// <summary>
    /// Legacy <c>cmbRunType</c> (line 607-617). The combo has no blank option and defaults to Regular;
    /// null here still means "do not filter", consistent with the rest of the JV family.
    /// </summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c> (line 621-629), matched by Code. Null means no filter.</summary>
    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "LineNumber ASC";
}

public class GetPayrollJVCustom042SalaryReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042AdvanceSalaryReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042AnnualLeaveProvisionReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042PublicHolidayLeaveProvisionReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042RamadanTimeBackLeaveProvisionReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042LieuDayLeaveProvisionReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042IndemnityProvisionReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042OvertimeReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042MissingAccountsReportInput : GetPayrollJVCustom042ReportInputBase
{
}

public class GetPayrollJVCustom042EmployeeWiseBreakDownReportInput : GetPayrollJVCustom042ReportInputBase
{
}

/// <summary>
/// One posting line shared by every "voucher-shaped" Custom 042 format (Salary, Advance Salary, the four
/// Provision variants, Overtime and EmployeeWise BreakDown) -- legacy's Main/Sub/Div/Dept/Anly1/Anly2/
/// Act1/Act2/Currency/Fc-amount/Net-amount/Dr-Cr column set (e.g. lines 987-1002), identical across all
/// nine of those branches. This is a KEY STRUCTURAL DIFFERENCE from the rest of the JV family (which uses
/// GLCode/GLName/Debit/Credit): dual-currency amounts plus separate Division/Department/Analysis/
/// Activity cost dimensions and an explicit Debit/Credit indicator instead of two signed columns.
/// </summary>
public class PayrollJVCustom042ReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid": a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "EmployeeCode" (EmployeeWise BreakDown only).</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>Legacy "Main".</summary>
    public string MainAccountCode { get; set; } = string.Empty;

    /// <summary>Legacy "Main descripton" [sic].</summary>
    public string MainAccountName { get; set; } = string.Empty;

    /// <summary>Legacy "Sub".</summary>
    public string SubAccountCode { get; set; } = string.Empty;

    /// <summary>Legacy "SubDescription".</summary>
    public string SubAccountName { get; set; } = string.Empty;

    /// <summary>Legacy "TD_DESC".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Legacy "Div" = <c>CostElement1</c>.</summary>
    public string Division { get; set; } = string.Empty;

    /// <summary>Legacy "Dept" = <c>CostElement2</c>.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Legacy "Anly1" = <c>CostElement3</c>.</summary>
    public string Analysis1 { get; set; } = string.Empty;

    /// <summary>Legacy "Anly2"/"Anl2". Never sourced from data -- always the branch's literal ("NULL" or "").</summary>
    public string Analysis2 { get; set; } = string.Empty;

    /// <summary>Legacy "Act1". Never sourced from data.</summary>
    public string Activity1 { get; set; } = string.Empty;

    /// <summary>Legacy "Act2". Never sourced from data.</summary>
    public string Activity2 { get; set; } = string.Empty;

    /// <summary>Legacy "Currency" -- always "KD" (see <see cref="PayrollJVPostingLineLoader"/> callers).</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Legacy "Fc amount" -- always equal to <see cref="NetAmount"/> in this screen (single currency).</summary>
    public double FcAmount { get; set; }

    /// <summary>Legacy "Net amount".</summary>
    public double NetAmount { get; set; }

    /// <summary>Legacy "Dr/Cr": "D" or "C".</summary>
    public string DrCr { get; set; } = string.Empty;
}

/// <summary>Legacy "Missing Accounts" branch (lines 3068-3137): salary lines with no wage-account mapping.</summary>
public class PayrollJVCustom042MissingAccountsRowDto : EntityDto<int>
{
    public int LineNumber { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public DateTime? ForPeriod { get; set; }

    public string WageTypeName { get; set; } = string.Empty;

    public string AccountInfoCode { get; set; } = string.Empty;

    public string AccountInfoName { get; set; } = string.Empty;

    public string OffsetAccountInfoCode { get; set; } = string.Empty;

    public string OffsetAccountInfoName { get; set; } = string.Empty;

    public string Division { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Analysis { get; set; } = string.Empty;

    public double CostPercentage { get; set; }

    public double Amount { get; set; }
}
