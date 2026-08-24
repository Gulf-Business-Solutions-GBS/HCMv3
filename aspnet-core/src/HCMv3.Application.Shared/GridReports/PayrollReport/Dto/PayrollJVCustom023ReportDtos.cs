using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// The legacy <c>cmbType</c> combo of the Payroll JV (Custom 023) screen
/// (<c>uscRptPayrollJV_Custom023.ascx.cs</c> <c>BindType()</c>, line 240). Each value produces a
/// COMPLETELY DIFFERENT <c>DataTable</c> from the same underlying data.
/// </summary>
public enum PayrollJVCustom023Format
{
    /// <summary>Legacy "Main Format" (line 688): GL-account-wise debit/credit summary.</summary>
    MainFormat = 0,

    /// <summary>Legacy "Missing Accounts" (line 936): salary lines with no wage-account mapping.</summary>
    MissingAccounts = 1,

    /// <summary>Legacy "EmployeeWise BreakDown" (line 1011): the same postings, per employee/wage type.</summary>
    EmployeeWiseBreakDown = 2
}

/// <summary>
/// Filters of the Payroll JV (Custom 023) report -- legacy <c>uscRptPayrollJV_Custom023.ascx</c>:
/// <c>txtMonth</c>, <c>txtYear</c>, <c>cmbRunType</c>, <c>cmbPayrollArea</c>, <c>cmbType</c> and
/// <c>txtEmpCode</c>.
///
/// The employee-code box IS live here (legacy lines 596-601), so the inherited
/// <see cref="GridReportInputBase.EmployeeCode"/> is used. The inherited date range and employee-status
/// combo are not part of this panel and are ignored.
/// </summary>
public class GetPayrollJVCustom023ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank meant "current month".</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank meant "current year".</summary>
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>, applied at legacy line 579.</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (matched by CODE at legacy line 587, then by the employee's org
    /// assignment covering the month end).
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy <c>cmbType</c>. Legacy defaulted the combo to "Main Format".</summary>
    public PayrollJVCustom023Format Format { get; set; } = PayrollJVCustom023Format.MainFormat;

    protected override string DefaultSorting => "LineNumber ASC";
}

/// <summary>
/// One line of the Payroll JV (Custom 023) voucher.
///
/// SUPERSET DTO: legacy <c>Table1()</c> builds three different <c>DataTable</c>s depending on
/// <c>cmbType</c> -- "Main Format" (7 columns, lines 694-709), "Missing Accounts" (14 columns, lines
/// 939-973) and "EmployeeWise BreakDown" (9 columns, lines 1016-1036). Rather than three services this
/// port returns one row type carrying every field, plus the server-side column list for the chosen
/// format (<see cref="PayrollJVCustom023ReportOutput.Columns"/>), so the grid and the xlsx show exactly
/// the legacy columns in the legacy order and nothing else.
/// </summary>
public class PayrollJVCustom023ReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid" column: a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    // --- Main Format (legacy lines 694-709) ---

    /// <summary>Legacy "Remark": the account name, or the fixed adjustment caption on the last line.</summary>
    public string Remark { get; set; }

    /// <summary>Legacy "GL Code" (Main Format) / "G/L Acct/BP Code" (EmployeeWise BreakDown).</summary>
    public string GLCode { get; set; }

    /// <summary>Legacy "Debit".</summary>
    public double Debit { get; set; }

    /// <summary>Legacy "Credit".</summary>
    public double Credit { get; set; }

    /// <summary>Legacy "Amount" (Main Format and Missing Accounts).</summary>
    public double Amount { get; set; }

    /// <summary>Legacy "GL Desc.": always empty string in legacy.</summary>
    public string GLDesc { get; set; } = string.Empty;

    // --- EmployeeWise BreakDown (legacy lines 1016-1036) ---

    /// <summary>Legacy "EmpCode".</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "WageType": the wage type's English name.</summary>
    public string WageTypeName { get; set; }

    /// <summary>Legacy "G/L Acct/BP Name".</summary>
    public string GLName { get; set; }

    /// <summary>Legacy "Control Acct": legacy always wrote empty string here.</summary>
    public string ControlAcct { get; set; } = string.Empty;

    /// <summary>Legacy "Remarks Template": always empty string in legacy.</summary>
    public string RemarksTemplate { get; set; } = string.Empty;

    // --- Missing Accounts (legacy lines 939-973) ---

    /// <summary>Legacy "ForPeriod".</summary>
    public DateTime? ForPeriod { get; set; }

    /// <summary>Legacy "AccountInfo Code".</summary>
    public string AccountInfoCode { get; set; } = string.Empty;

    /// <summary>Legacy "AccountInfo Name".</summary>
    public string AccountInfoName { get; set; } = string.Empty;

    /// <summary>Legacy "OffsetAccountInfo Code".</summary>
    public string OffsetAccountInfoCode { get; set; } = string.Empty;

    /// <summary>Legacy "OffsetAccountInfo Name".</summary>
    public string OffsetAccountInfoName { get; set; } = string.Empty;

    /// <summary>Legacy "Line of Business" = <c>CostElement1</c>.</summary>
    public string LineOfBusiness { get; set; } = string.Empty;

    /// <summary>Legacy "Vehicle/Machinery" = <c>CostElement2</c>.</summary>
    public string VehicleMachinery { get; set; } = string.Empty;

    /// <summary>Legacy "Departments" = <c>CostElement3</c>.</summary>
    public string Departments { get; set; } = string.Empty;

    /// <summary>Legacy "Project" = <c>CostElement4</c>.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Legacy "CostPercentage".</summary>
    public double CostPercentage { get; set; }
}

/// <summary>Paged result plus the column list for the chosen <c>cmbType</c> format.</summary>
public class PayrollJVCustom023ReportOutput : PagedResultDto<PayrollJVCustom023ReportRowDto>
{
    public List<PayrollJVReportColumnDto> Columns { get; set; } = new();
}
