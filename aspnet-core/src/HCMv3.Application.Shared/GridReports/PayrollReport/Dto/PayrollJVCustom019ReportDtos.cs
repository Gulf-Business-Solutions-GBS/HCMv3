using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// The legacy <c>cmbType</c> combo of the Payroll JV (Custom 019) screen
/// (<c>uscRptPayrollJV_Custom019.ascx.cs</c> <c>BindType()</c>, line 241). Each value produces a
/// different <c>DataTable</c> from the same underlying data.
/// </summary>
public enum PayrollJVCustom019Format
{
    /// <summary>Legacy "Main Format" (line 802): GL-account-wise debit/credit summary.</summary>
    MainFormat = 0,

    /// <summary>
    /// Legacy "Provision Format" (line 950): provision wage types posted as an account/offset-account
    /// pair. This branch is LIVE in Custom 019 -- its twin is commented out in Custom 023.
    /// </summary>
    ProvisionFormat = 1,

    /// <summary>Legacy "Missing Accounts" (line 1039): salary lines with no wage-account mapping.</summary>
    MissingAccounts = 2,

    /// <summary>Legacy "EmployeeWise BreakDown" (line 1113): the same postings, per employee/wage type.</summary>
    EmployeeWiseBreakDown = 3
}

/// <summary>
/// Legacy <c>cmbPayrollArea</c> of the Custom 019 screen. UNUSUALLY, this combo does not pick a payroll
/// area at all -- <c>BindPayrollArea()</c> (line 199) binds exactly two hard-coded entries and
/// <c>Table1()</c> (lines 592-601) turns them into a NAME test against the literal "Sub-Contractors".
/// </summary>
public enum PayrollJVCustom019PayrollAreaScope
{
    /// <summary>Legacy code "0": every payroll area EXCEPT the one named "Sub-Contractors".</summary>
    AllExceptSubContractors = 0,

    /// <summary>Legacy code "1": only the payroll area named "Sub-Contractors".</summary>
    SubContractorsOnly = 1
}

/// <summary>
/// Filters of the Payroll JV (Custom 019) report -- legacy <c>uscRptPayrollJV_Custom019.ascx</c>:
/// <c>txtMonth</c>, <c>txtYear</c>, <c>cmbRunType</c>, <c>cmbPayrollArea</c>, <c>cmbType</c> and
/// <c>txtEmpCode</c>. The inherited date range and employee-status combo are not part of this panel and
/// are ignored; the inherited <see cref="GridReportInputBase.EmployeeCode"/> IS used (legacy lines
/// 616-622).
/// </summary>
public class GetPayrollJVCustom019ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank meant "current month".</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank meant "current year".</summary>
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>, applied at legacy line 586.</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>. Null means the combo was left blank: no filter at all.</summary>
    public PayrollJVCustom019PayrollAreaScope? PayrollAreaScope { get; set; }

    /// <summary>Legacy <c>cmbType</c>. Legacy defaulted the combo to "Main Format".</summary>
    public PayrollJVCustom019Format Format { get; set; } = PayrollJVCustom019Format.MainFormat;

    protected override string DefaultSorting => "LineNumber ASC";
}

/// <summary>
/// One line of the Payroll JV (Custom 019) voucher.
///
/// SUPERSET DTO: legacy <c>Table1()</c> builds four different <c>DataTable</c>s depending on
/// <c>cmbType</c>. This port returns one row type carrying every field plus the server-side column list
/// for the chosen format (<see cref="PayrollJVCustom019ReportOutput.Columns"/>), so the grid and the
/// xlsx show exactly the legacy columns in the legacy order and nothing else.
/// </summary>
public class PayrollJVCustom019ReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid" column: a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "EmpCode" (Missing Accounts and EmployeeWise BreakDown only).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "WageType": the wage type's English name.</summary>
    public string WageTypeName { get; set; }

    /// <summary>Legacy "G/L Acct/BP Code".</summary>
    public string GLCode { get; set; }

    /// <summary>Legacy "G/L Acct/BP Name".</summary>
    public string GLName { get; set; }

    /// <summary>
    /// Legacy "Control Acct". Blank on the Main Format and EmployeeWise branches (legacy lines
    /// 858/896/946 write <c>""</c> with the real value commented out) and the account code on the
    /// Provision Format branch (lines 1005/1024).
    /// </summary>
    public string ControlAcct { get; set; } = string.Empty;

    /// <summary>Legacy "Debit".</summary>
    public double Debit { get; set; }

    /// <summary>Legacy "Credit".</summary>
    public double Credit { get; set; }

    /// <summary>Legacy "Remarks Template": always empty string in legacy.</summary>
    public string RemarksTemplate { get; set; } = string.Empty;

    /// <summary>Legacy "Project" = <c>CostElement4</c>.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Legacy "Line of Business" = <c>CostElement1</c>.</summary>
    public string LineOfBusiness { get; set; } = string.Empty;

    /// <summary>
    /// Legacy "Vehicle/Machinery" = <c>CostElement2</c>.
    ///
    /// LEGACY DEFECT REPRODUCED: this column is declared on ALL FOUR formats and is grouped by, but
    /// legacy only ever WRITES it on the "Missing Accounts" branch (line 1098). On Main Format,
    /// Provision Format and EmployeeWise BreakDown it is left blank -- a cost dimension silently dropped
    /// from a journal voucher. See the app service remarks; flagged for a customer decision.
    /// </summary>
    public string VehicleMachinery { get; set; } = string.Empty;

    /// <summary>Legacy "Departments" = <c>CostElement3</c>.</summary>
    public string Departments { get; set; } = string.Empty;

    // --- Missing Accounts (legacy lines 1042-1076) ---

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

    /// <summary>Legacy "CostPercentage".</summary>
    public double CostPercentage { get; set; }

    /// <summary>Legacy "Amount" (Missing Accounts only).</summary>
    public double Amount { get; set; }
}

/// <summary>Paged result plus the column list for the chosen <c>cmbType</c> format.</summary>
public class PayrollJVCustom019ReportOutput : PagedResultDto<PayrollJVCustom019ReportRowDto>
{
    public List<PayrollJVReportColumnDto> Columns { get; set; } = new();
}
