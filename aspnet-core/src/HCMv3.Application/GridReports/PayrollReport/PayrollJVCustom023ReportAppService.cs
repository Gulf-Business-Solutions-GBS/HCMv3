using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 023) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom023.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 557-1176).
///
/// SHAPE: a journal voucher driven by the employee's <c>EmployeeWageAccount</c> mapping. The month's
/// salary-register lines are left-joined to that mapping (shared with Custom 019 through
/// <see cref="PayrollJVPostingLineLoader"/>), then aggregated into GL postings. The legacy
/// <c>cmbType</c> combo picks between three COMPLETELY DIFFERENT tables, so this service returns a
/// superset row plus the column list for the chosen format:
/// <list type="bullet">
/// <item><description>"Main Format" (legacy lines 688-844): account-wise debit/credit summary plus a
/// balancing "Salary Due to be Adjusted on Leave Resumption" line.</description></item>
/// <item><description>"Missing Accounts" (legacy lines 936-1008): the salary lines that have NO wage
/// account mapped -- an exception report, not a posting.</description></item>
/// <item><description>"EmployeeWise BreakDown" (legacy lines 1011-1167): the same postings, one row per
/// employee/wage type.</description></item>
/// </list>
/// Legacy's fourth branch, "Provision Format", is COMMENTED OUT in this control (lines 848-933) and is
/// therefore not offered here. Its live twin exists in Custom 019.
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
///
/// ACCOUNTING DEFECT FLAGGED, NOT FIXED: cost percentages are ignored and cost splits double-count. See
/// the <see cref="PayrollJVPostingLineLoader"/> class remarks and the pinning test
/// <c>Should_Reproduce_The_Legacy_Defect_Where_Cost_Splits_Multiply_The_Amount</c>.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down/drill-up session
/// stack, the ColumnChooser JSON persistence (lines 1977-2017), the callback-panel state machine
/// (lines 1815-1938), the PDF export button (line 1943) and the dead <c>Table1_Old()</c> (lines
/// 1177-1814).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom023)]
public class PayrollJVCustom023ReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom023ReportAppService
{
    /// <summary>Legacy lines 700/739/765: the Net Pay wage type, handled separately from earnings.</summary>
    private const string NetPayWageTypeCode = "9000";

    /// <summary>Legacy line 823: the caption of the balancing line.</summary>
    private const string AdjustmentRemark = "Salary Due to be Adjusted on Leave Resumption";

    private readonly PayrollJVPostingLineLoader _postingLineLoader;

    public PayrollJVCustom023ReportAppService(PayrollJVPostingLineLoader postingLineLoader)
    {
        _postingLineLoader = postingLineLoader;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom023";

    protected override string ScreenCaption => "Payroll JV (Custom 023)";

    [HttpPost]
    public async Task<PayrollJVCustom023ReportOutput> GetPayrollJVCustom023Report(
        GetPayrollJVCustom023ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PayrollJVCustom023ReportOutput
        {
            TotalCount = rows.Count,
            Items = paged,
            Columns = GetColumnDtos(input.Format)
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom023ReportToExcel(GetPayrollJVCustom023ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom023.xlsx",
            ordered,
            GetExportColumns(input.Format),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy <c>Table1()</c> (lines 557-1176).
    /// </summary>
    private async Task<List<PayrollJVCustom023ReportRowDto>> BuildRowsAsync(
        GetPayrollJVCustom023ReportInput input)
    {
        var endDate = ResolveEndDate(input);

        var lines = await _postingLineLoader.LoadAsync(
            endDate,
            // Legacy 023 admits only Earning/Deduction billing types (line 566-568); no extra codes.
            Array.Empty<string>(),
            input.PayrollRunType,
            input.PayrollAreaId.HasValue ? new[] { input.PayrollAreaId.Value } : Array.Empty<int>(),
            input.ResolveEmployeeCodes(),
            await GetRlsEmployeeIdsAsync());

        return input.Format switch
        {
            PayrollJVCustom023Format.MissingAccounts => BuildMissingAccountsRows(lines),
            PayrollJVCustom023Format.EmployeeWiseBreakDown => BuildEmployeeWiseRows(lines),
            _ => BuildMainFormatRows(lines)
        };
    }

    /// <summary>Legacy "Main Format" branch, lines 688-844.</summary>
    private static List<PayrollJVCustom023ReportRowDto> BuildMainFormatRows(List<PayrollJVPostingLine> lines)
    {
        var rows = new List<PayrollJVCustom023ReportRowDto>();

        // Legacy lines 711-738 -- Debit entries: earnings other than Net Pay, grouped by account and
        // cost elements 1-4.
        foreach (var group in GroupByAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Earning
                                      && l.WageTypeCode != NetPayWageTypeCode)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();
            rows.Add(NewRow(rows, sample.AccountInfoName, sample.AccountInfoCode,
                debit: amount >= 0 ? amount : 0,
                credit: amount < 0 ? -amount : 0,
                amount: amount));
        }

        // Legacy lines 741-768 -- Credit entries: deductions. Note the Amount column is NEGATED here.
        foreach (var group in GroupByAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Deduction)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();
            rows.Add(NewRow(rows, sample.AccountInfoName, sample.AccountInfoCode,
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0,
                amount: -amount));
        }

        // Legacy lines 771-815 -- Net Salary entries. Legacy drops zero-amount lines BEFORE grouping and
        // again after, which changes nothing but is kept for readability of the diff.
        foreach (var group in GroupByAccountAndCostElements(
                     lines.Where(l => l.WageTypeCode == NetPayWageTypeCode && l.Amount != 0)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();
            rows.Add(NewRow(rows, sample.AccountInfoName, sample.AccountInfoCode,
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0,
                amount: -amount));
        }

        // Legacy lines 817-843 -- the balancing line. Net Pay minus (earnings - deductions) that impact
        // net pay; when it is non-zero the difference is booked with NO GL account at all.
        var totalNetPay = lines.Where(l => l.WageTypeCode == NetPayWageTypeCode).Sum(l => l.Amount);
        var totalEarning = lines
            .Where(l => l.BillingType == BillingType.Earning
                        && !l.DontImpactNetPay
                        && l.WageTypeCode != NetPayWageTypeCode)
            .Sum(l => l.Amount);
        var totalDeduction = lines
            .Where(l => l.BillingType == BillingType.Deduction && !l.DontImpactNetPay)
            .Sum(l => l.Amount);

        var adjustment = totalNetPay - (totalEarning - totalDeduction);
        if (adjustment != 0)
        {
            rows.Add(NewRow(rows, AdjustmentRemark, string.Empty,
                debit: adjustment >= 0 ? adjustment : 0,
                credit: adjustment < 0 ? -adjustment : 0,
                amount: adjustment));
        }

        return rows;
    }

    /// <summary>Legacy "Missing Accounts" branch, lines 936-1008.</summary>
    private static List<PayrollJVCustom023ReportRowDto> BuildMissingAccountsRows(List<PayrollJVPostingLine> lines)
    {
        var rows = new List<PayrollJVCustom023ReportRowDto>();

        foreach (var line in lines.Where(l => l.AccountInfoId == null))
        {
            rows.Add(new PayrollJVCustom023ReportRowDto
            {
                Id = rows.Count + 1,
                LineNumber = rows.Count + 1,
                EmployeeCode = line.EmployeeCode,
                ForPeriod = line.ForPeriod,
                WageTypeName = line.WageTypeName,
                // Legacy still writes these four, and they are always "" here by construction --
                // the branch selects exactly the lines whose AccountInfo is null.
                AccountInfoCode = line.AccountInfoCode ?? string.Empty,
                AccountInfoName = line.AccountInfoName ?? string.Empty,
                OffsetAccountInfoCode = line.OffsetAccountInfoCode ?? string.Empty,
                OffsetAccountInfoName = line.OffsetAccountInfoName ?? string.Empty,
                LineOfBusiness = line.CostElement1Code ?? string.Empty,
                VehicleMachinery = line.CostElement2Code ?? string.Empty,
                Departments = line.CostElement3Code ?? string.Empty,
                Project = line.CostElement4Code ?? string.Empty,
                CostPercentage = line.CostPercentage,
                Amount = line.Amount
            });
        }

        return rows;
    }

    /// <summary>Legacy "EmployeeWise BreakDown" branch, lines 1011-1167.</summary>
    private static List<PayrollJVCustom023ReportRowDto> BuildEmployeeWiseRows(List<PayrollJVPostingLine> lines)
    {
        var rows = new List<PayrollJVCustom023ReportRowDto>();

        // Legacy lines 1038-1073 -- Debit entries.
        foreach (var group in GroupByEmployeeWageTypeAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Earning
                                      && l.WageTypeCode != NetPayWageTypeCode)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            rows.Add(NewEmployeeWiseRow(rows, group.First(),
                debit: amount >= 0 ? amount : 0,
                credit: amount < 0 ? -amount : 0));
        }

        // Legacy lines 1076-1111 -- Credit entries.
        foreach (var group in GroupByEmployeeWageTypeAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Deduction)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            rows.Add(NewEmployeeWiseRow(rows, group.First(),
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0));
        }

        // Legacy lines 1114-1166 -- Net Salary entries.
        foreach (var group in GroupByEmployeeWageTypeAccountAndCostElements(
                     lines.Where(l => l.WageTypeCode == NetPayWageTypeCode && l.Amount != 0)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            rows.Add(NewEmployeeWiseRow(rows, group.First(),
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0));
        }

        return rows;
    }

    /// <summary>
    /// Legacy grouped by the XPO <c>AccountInfo</c>/<c>CostElementN</c> OBJECTS, i.e. by identity, with
    /// all nulls falling into one group. Grouping by the nullable FK ids is the same thing.
    /// </summary>
    private static IEnumerable<IGrouping<(int?, int?, int?, int?, int?), PayrollJVPostingLine>>
        GroupByAccountAndCostElements(IEnumerable<PayrollJVPostingLine> lines)
    {
        return lines.GroupBy(l => (l.AccountInfoId, l.CostElement1Id, l.CostElement2Id, l.CostElement3Id,
            l.CostElement4Id));
    }

    private static IEnumerable<IGrouping<(int, int, int?, int?, int?, int?, int?), PayrollJVPostingLine>>
        GroupByEmployeeWageTypeAccountAndCostElements(IEnumerable<PayrollJVPostingLine> lines)
    {
        return lines.GroupBy(l => (l.EmployeeId, l.WageTypeId, l.AccountInfoId, l.CostElement1Id,
            l.CostElement2Id, l.CostElement3Id, l.CostElement4Id));
    }

    private static PayrollJVCustom023ReportRowDto NewRow(
        List<PayrollJVCustom023ReportRowDto> rows,
        string remark,
        string glCode,
        double debit,
        double credit,
        double amount)
    {
        return new PayrollJVCustom023ReportRowDto
        {
            Id = rows.Count + 1,
            LineNumber = rows.Count + 1,
            Remark = remark ?? string.Empty,
            GLCode = glCode ?? string.Empty,
            Debit = debit,
            Credit = credit,
            Amount = amount,
            GLDesc = string.Empty
        };
    }

    private static PayrollJVCustom023ReportRowDto NewEmployeeWiseRow(
        List<PayrollJVCustom023ReportRowDto> rows,
        PayrollJVPostingLine sample,
        double debit,
        double credit)
    {
        return new PayrollJVCustom023ReportRowDto
        {
            Id = rows.Count + 1,
            LineNumber = rows.Count + 1,
            EmployeeCode = sample.EmployeeCode,
            WageTypeName = sample.WageTypeName,
            GLCode = sample.AccountInfoCode ?? string.Empty,
            GLName = sample.AccountInfoName ?? string.Empty,
            // Legacy explicitly blanks Control Acct on this branch (lines 1063/1101/1152).
            ControlAcct = string.Empty,
            Debit = debit,
            Credit = credit,
            RemarksTemplate = string.Empty
        };
    }

    /// <summary>Legacy <c>GetTable()</c>: blank month/year mean "now"; an invalid pair fell back to 1900.</summary>
    private DateTime ResolveEndDate(GetPayrollJVCustom023ReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(1900, 1, 31).Date;
        }
    }

    /// <summary>Legacy <c>GetParameterStr()</c>: Payroll Area | Run Type | Month | Year | Type | Emp Code.</summary>
    private string BuildJvParameterString(GetPayrollJVCustom023ReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));
        parts.Add("Type : " + input.Format);

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The legacy column list of the chosen format, in legacy declaration order. The same list drives
    /// the Angular grid and <see cref="GetExportColumns"/>, so the two cannot drift.
    /// </summary>
    private static List<PayrollJVReportColumnDto> GetColumnDtos(PayrollJVCustom023Format format)
    {
        switch (format)
        {
            // Legacy lines 939-973.
            case PayrollJVCustom023Format.MissingAccounts:
                return new List<PayrollJVReportColumnDto>
                {
                    new("lineNumber", "SrNo"),
                    new("employeeCode", "EmpCode"),
                    new("forPeriod", "ForPeriod"),
                    new("wageTypeName", "WageType"),
                    new("accountInfoCode", "AccountInfoCode"),
                    new("accountInfoName", "AccountInfoName"),
                    new("offsetAccountInfoCode", "OffsetAccountInfoCode"),
                    new("offsetAccountInfoName", "OffsetAccountInfoName"),
                    new("project", "Project"),
                    new("lineOfBusiness", "LineOfBusiness"),
                    new("vehicleMachinery", "VehicleMachinery"),
                    new("departments", "Departments"),
                    new("costPercentage", "CostPercentage"),
                    new("amount", "Amount")
                };

            // Legacy lines 1016-1036.
            case PayrollJVCustom023Format.EmployeeWiseBreakDown:
                return new List<PayrollJVReportColumnDto>
                {
                    new("lineNumber", "SrNo"),
                    new("employeeCode", "EmpCode"),
                    new("wageTypeName", "WageType"),
                    new("glCode", "GLAcctBPCode"),
                    new("glName", "GLAcctBPName"),
                    new("controlAcct", "ControlAcct"),
                    new("debit", "Debit"),
                    new("credit", "Credit"),
                    new("remarksTemplate", "RemarksTemplate")
                };

            // Legacy lines 694-709.
            default:
                return new List<PayrollJVReportColumnDto>
                {
                    new("lineNumber", "SrNo"),
                    new("remark", "Remark"),
                    new("glCode", "GLCode"),
                    new("debit", "Debit"),
                    new("credit", "Credit"),
                    new("amount", "Amount"),
                    new("glDesc", "GLDesc")
                };
        }
    }

    private static IReadOnlyList<GridReportColumn<PayrollJVCustom023ReportRowDto>> GetExportColumns(
        PayrollJVCustom023Format format)
    {
        switch (format)
        {
            case PayrollJVCustom023Format.MissingAccounts:
                return new List<GridReportColumn<PayrollJVCustom023ReportRowDto>>
                {
                    new("SrNo", r => r.LineNumber),
                    new("EmpCode", r => r.EmployeeCode),
                    new("ForPeriod", r => r.ForPeriod),
                    new("WageType", r => r.WageTypeName),
                    new("AccountInfoCode", r => r.AccountInfoCode),
                    new("AccountInfoName", r => r.AccountInfoName),
                    new("OffsetAccountInfoCode", r => r.OffsetAccountInfoCode),
                    new("OffsetAccountInfoName", r => r.OffsetAccountInfoName),
                    new("Project", r => r.Project),
                    new("LineOfBusiness", r => r.LineOfBusiness),
                    new("VehicleMachinery", r => r.VehicleMachinery),
                    new("Departments", r => r.Departments),
                    new("CostPercentage", r => r.CostPercentage),
                    new("Amount", r => r.Amount)
                };

            case PayrollJVCustom023Format.EmployeeWiseBreakDown:
                return new List<GridReportColumn<PayrollJVCustom023ReportRowDto>>
                {
                    new("SrNo", r => r.LineNumber),
                    new("EmpCode", r => r.EmployeeCode),
                    new("WageType", r => r.WageTypeName),
                    new("GLAcctBPCode", r => r.GLCode),
                    new("GLAcctBPName", r => r.GLName),
                    new("ControlAcct", r => r.ControlAcct),
                    new("Debit", r => r.Debit),
                    new("Credit", r => r.Credit),
                    new("RemarksTemplate", r => r.RemarksTemplate)
                };

            default:
                return new List<GridReportColumn<PayrollJVCustom023ReportRowDto>>
                {
                    new("SrNo", r => r.LineNumber),
                    new("Remark", r => r.Remark),
                    new("GLCode", r => r.GLCode),
                    new("Debit", r => r.Debit),
                    new("Credit", r => r.Credit),
                    new("Amount", r => r.Amount),
                    new("GLDesc", r => r.GLDesc)
                };
        }
    }
}
