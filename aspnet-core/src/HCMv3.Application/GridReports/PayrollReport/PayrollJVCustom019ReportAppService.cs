using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 019) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom019.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 558-1296).
///
/// SHAPE: the same wage-account-driven journal voucher as Custom 023 -- the shared salary-register /
/// <c>EmployeeWageAccount</c> left join lives in <see cref="PayrollJVPostingLineLoader"/> -- with three
/// customer-specific differences:
/// <list type="number">
/// <item><description>Provision wage types 3001 (Annual Leave Provision) and 3000 (Indemnity Provision)
/// are admitted alongside Earning/Deduction (legacy lines 562-566), and the "Provision Format" branch
/// (lines 950-1036) is LIVE here, posting each provision as an account/offset-account pair. The
/// equivalent branch is commented out in Custom 023.</description></item>
/// <item><description>The Payroll Area combo is NOT a payroll-area picker: it offers exactly two
/// choices, "everything except the payroll area NAMED Sub-Contractors" and "only Sub-Contractors"
/// (legacy lines 592-601). See <see cref="PayrollJVCustom019PayrollAreaScope"/>.</description></item>
/// <item><description>A SYNTHETIC posting line per employee whose Net Pay came out as exactly zero:
/// legacy (lines 644-706) re-reads every net-pay-impacting line of those registers, signs deductions
/// negative, sums them per employee, and books the total against the "Last Month Recovery Ded" wage type
/// (code 2011). See <see cref="BuildLastMonthRecoveryLinesAsync"/>.</description></item>
/// </list>
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
///
/// ACCOUNTING DEFECTS FLAGGED, NOT FIXED -- both reproduced verbatim and pinned by tests:
/// <list type="number">
/// <item><description>THE "Vehicle/Machinery" COST DIMENSION IS DROPPED. All four formats declare the
/// column and group by <c>CostElement2</c>, but legacy writes it only on the "Missing Accounts" branch
/// (line 1098). On the three POSTING formats it is always blank, so a journal voucher goes to finance
/// with one of its four cost dimensions silently missing.</description></item>
/// <item><description>COST PERCENTAGES ARE IGNORED AND COST SPLITS DOUBLE-COUNT -- see the
/// <see cref="PayrollJVPostingLineLoader"/> class remarks.</description></item>
/// </list>
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down/drill-up session
/// stack, the ColumnChooser JSON persistence (lines 2186-2226), the callback-panel state machine
/// (lines 2024-2147), the PDF export button (line 2152) and the dead <c>Table1_Old()</c> (lines
/// 1297-2023).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom019)]
public class PayrollJVCustom019ReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom019ReportAppService
{
    /// <summary>Legacy lines 562-566: Annual Leave Provision and Indemnity Provision.</summary>
    private static readonly string[] ProvisionWageTypeCodes = { "3001", "3000" };

    /// <summary>Legacy lines 810/838: the Net Pay wage type, handled separately from earnings.</summary>
    private const string NetPayWageTypeCode = "9000";

    /// <summary>Legacy lines 646-649: the wage type the synthetic zero-net-pay line is booked against.</summary>
    private const string LastMonthRecoveryDedWageTypeCode = "2011";

    /// <summary>Legacy line 596/598: the payroll area singled out by NAME, not by code.</summary>
    private const string SubContractorsPayrollAreaName = "Sub-Contractors";

    private readonly PayrollJVPostingLineLoader _postingLineLoader;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public PayrollJVCustom019ReportAppService(
        PayrollJVPostingLineLoader postingLineLoader,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _postingLineLoader = postingLineLoader;
        _payrollAreaRepository = payrollAreaRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom019";

    protected override string ScreenCaption => "Payroll JV (Custom 019)";

    [HttpPost]
    public async Task<PayrollJVCustom019ReportOutput> GetPayrollJVCustom019Report(
        GetPayrollJVCustom019ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PayrollJVCustom019ReportOutput
        {
            TotalCount = rows.Count,
            Items = paged,
            Columns = GetColumnDtos(input.Format)
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom019ReportToExcel(GetPayrollJVCustom019ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom019.xlsx",
            ordered,
            GetExportColumns(input.Format),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy <c>Table1()</c> (lines 558-1296).
    /// </summary>
    private async Task<List<PayrollJVCustom019ReportRowDto>> BuildRowsAsync(
        GetPayrollJVCustom019ReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var payrollAreaIds = await ResolvePayrollAreaIdsAsync(input.PayrollAreaScope);

        var salaryLines = await _postingLineLoader.LoadSalaryLinesAsync(
            endDate,
            ProvisionWageTypeCodes,
            input.PayrollRunType,
            payrollAreaIds,
            employeeCodes,
            rlsEmployeeIds);

        var wageAccountLookup = await _postingLineLoader.LoadWageAccountLookupAsync(endDate, employeeCodes);

        var lines = _postingLineLoader.Join(salaryLines, wageAccountLookup);

        // Legacy lines 644-706 + 762-786: the synthetic zero-net-pay lines are joined to the SAME wage
        // account lookup and concatenated onto the main set.
        var recoveryLines = await BuildLastMonthRecoveryLinesAsync(salaryLines);
        lines.AddRange(_postingLineLoader.Join(recoveryLines, wageAccountLookup));

        return input.Format switch
        {
            PayrollJVCustom019Format.ProvisionFormat => BuildProvisionFormatRows(lines),
            PayrollJVCustom019Format.MissingAccounts => BuildMissingAccountsRows(lines),
            PayrollJVCustom019Format.EmployeeWiseBreakDown => BuildEmployeeWiseRows(lines),
            _ => BuildMainFormatRows(lines)
        };
    }

    /// <summary>
    /// Legacy lines 644-706. When an employee's Net Pay line came out as exactly 0, legacy re-reads
    /// EVERY net-pay-impacting Earning/Deduction line of that register -- note: WITHOUT the
    /// <c>IncludeInFIPosting</c> restriction the main query applies -- signs deductions negative, sums
    /// them per employee and period, and books the total against wage type 2011 "Last Month Recovery
    /// Ded". When wage type 2011 does not exist in master data legacy still emitted the line, with an
    /// empty wage type and billing type <c>TimeType</c> (lines 690-696); that is reproduced, and such a
    /// line then falls out of every posting branch because it is neither an Earning nor a Deduction.
    /// </summary>
    private async Task<List<PayrollJVSalaryLine>> BuildLastMonthRecoveryLinesAsync(
        List<PayrollJVSalaryLine> salaryLines)
    {
        var zeroNetPayRegisterIds = salaryLines
            .Where(l => l.WageTypeCode == NetPayWageTypeCode && l.Amount == 0)
            .Select(l => l.SalaryRegisterId)
            .Distinct()
            .ToList();

        if (zeroNetPayRegisterIds.Count == 0)
        {
            return new List<PayrollJVSalaryLine>();
        }

        var contributions = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => zeroNetPayRegisterIds.Contains(d.SalaryRegisterId)
                        && !d.WageType.DontImpactNetPay
                        && (d.WageType.BillingType == BillingType.Earning
                            || d.WageType.BillingType == BillingType.Deduction))
            .Select(d => new
            {
                d.SalaryRegisterId,
                d.SalaryRegister.ForPeriod,
                d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                EmployeeName = d.SalaryRegister.Employee.FullNameEn,
                d.WageType.BillingType,
                d.Amount
            })
            .ToListAsync();

        var recoveryWageType = await _wageTypeRepository.GetAll()
            .Where(w => w.Code == LastMonthRecoveryDedWageTypeCode)
            .Select(w => new
            {
                w.Id,
                w.Code,
                w.NameEn,
                w.BillingType,
                w.DontImpactNetPay,
                w.PayrollOrder
            })
            .FirstOrDefaultAsync();

        return contributions
            // Legacy lines 674-681: deductions count negative.
            .Select(c => new
            {
                c.SalaryRegisterId,
                c.EmployeeId,
                c.EmployeeCode,
                c.EmployeeName,
                c.ForPeriod,
                Amount = c.BillingType == BillingType.Deduction ? -1 * c.Amount : c.Amount
            })
            .GroupBy(c => new { c.EmployeeId, c.EmployeeCode, c.EmployeeName, c.ForPeriod })
            .Select(g => new PayrollJVSalaryLine
            {
                SalaryRegisterDetailId = 0,
                SalaryRegisterId = g.First().SalaryRegisterId,
                ForPeriod = g.Key.ForPeriod,
                EmployeeId = g.Key.EmployeeId,
                EmployeeCode = g.Key.EmployeeCode,
                EmployeeName = g.Key.EmployeeName,
                WageTypeId = recoveryWageType?.Id ?? 0,
                WageTypeCode = recoveryWageType?.Code ?? string.Empty,
                WageTypeName = recoveryWageType?.NameEn ?? string.Empty,
                BillingType = recoveryWageType?.BillingType ?? BillingType.TimeType,
                DontImpactNetPay = recoveryWageType?.DontImpactNetPay ?? false,
                PayrollOrder = recoveryWageType?.PayrollOrder ?? 0,
                Amount = g.Sum(x => x.Amount)
            })
            .ToList();
    }

    /// <summary>Legacy "Main Format" branch, lines 802-947.</summary>
    private static List<PayrollJVCustom019ReportRowDto> BuildMainFormatRows(List<PayrollJVPostingLine> lines)
    {
        var rows = new List<PayrollJVCustom019ReportRowDto>();

        // Legacy lines 825-861 -- Debit entries: earnings other than Net Pay.
        foreach (var group in GroupByAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Earning
                                      && l.WageTypeCode != NetPayWageTypeCode)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            rows.Add(NewPostingRow(rows, group.First(),
                debit: amount >= 0 ? amount : 0,
                credit: amount < 0 ? -amount : 0));
        }

        // Legacy lines 864-897 -- Credit entries: deductions.
        foreach (var group in GroupByAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Deduction)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            rows.Add(NewPostingRow(rows, group.First(),
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0));
        }

        // Legacy lines 900-946 -- Net Salary entries.
        foreach (var group in GroupByAccountAndCostElements(
                     lines.Where(l => l.WageTypeCode == NetPayWageTypeCode && l.Amount != 0)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            rows.Add(NewPostingRow(rows, group.First(),
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0));
        }

        return rows;
    }

    /// <summary>
    /// Legacy "Provision Format" branch, lines 950-1036: each provision group produces TWO rows, the
    /// debit against the account and the mirrored credit against the offset account. Unlike the other
    /// three formats, this branch DOES fill Control Acct (lines 1005/1024).
    /// </summary>
    private static List<PayrollJVCustom019ReportRowDto> BuildProvisionFormatRows(List<PayrollJVPostingLine> lines)
    {
        var rows = new List<PayrollJVCustom019ReportRowDto>();

        var groups = lines
            .Where(l => l.BillingType == BillingType.Provision)
            .GroupBy(l => (l.AccountInfoId, l.OffsetAccountInfoId, l.CostElement1Id, l.CostElement2Id,
                l.CostElement3Id, l.CostElement4Id));

        foreach (var group in groups)
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();

            // Legacy lines 993-1012 -- Debit against AccountInfo.
            rows.Add(NewPostingRow(rows, sample,
                debit: amount >= 0 ? amount : 0,
                credit: amount < 0 ? -amount : 0,
                controlAcct: sample.AccountInfoCode ?? string.Empty));

            // Legacy lines 1014-1033 -- Credit against OffsetAccountInfo.
            var offsetRow = NewPostingRow(rows, sample,
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0,
                controlAcct: sample.OffsetAccountInfoCode ?? string.Empty);
            offsetRow.GLCode = sample.OffsetAccountInfoCode ?? string.Empty;
            offsetRow.GLName = sample.OffsetAccountInfoName ?? string.Empty;
            rows.Add(offsetRow);
        }

        return rows;
    }

    /// <summary>Legacy "Missing Accounts" branch, lines 1039-1110.</summary>
    private static List<PayrollJVCustom019ReportRowDto> BuildMissingAccountsRows(List<PayrollJVPostingLine> lines)
    {
        var rows = new List<PayrollJVCustom019ReportRowDto>();

        foreach (var line in lines.Where(l => l.AccountInfoId == null))
        {
            rows.Add(new PayrollJVCustom019ReportRowDto
            {
                Id = rows.Count + 1,
                LineNumber = rows.Count + 1,
                EmployeeCode = line.EmployeeCode,
                ForPeriod = line.ForPeriod,
                WageTypeName = line.WageTypeName,
                // Always "" here by construction: the branch selects the lines whose account is null.
                AccountInfoCode = line.AccountInfoCode ?? string.Empty,
                AccountInfoName = line.AccountInfoName ?? string.Empty,
                OffsetAccountInfoCode = line.OffsetAccountInfoCode ?? string.Empty,
                OffsetAccountInfoName = line.OffsetAccountInfoName ?? string.Empty,
                LineOfBusiness = line.CostElement1Code ?? string.Empty,
                // The ONLY branch on which legacy writes Vehicle/Machinery (line 1098).
                VehicleMachinery = line.CostElement2Code ?? string.Empty,
                Departments = line.CostElement3Code ?? string.Empty,
                Project = line.CostElement4Code ?? string.Empty,
                CostPercentage = line.CostPercentage,
                Amount = line.Amount
            });
        }

        return rows;
    }

    /// <summary>Legacy "EmployeeWise BreakDown" branch, lines 1113-1287.</summary>
    private static List<PayrollJVCustom019ReportRowDto> BuildEmployeeWiseRows(List<PayrollJVPostingLine> lines)
    {
        var rows = new List<PayrollJVCustom019ReportRowDto>();

        // Legacy lines 1143-1183 -- Debit entries.
        foreach (var group in GroupByEmployeeWageTypeAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Earning
                                      && l.WageTypeCode != NetPayWageTypeCode)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var row = NewPostingRow(rows, group.First(),
                debit: amount >= 0 ? amount : 0,
                credit: amount < 0 ? -amount : 0);
            StampEmployeeAndWageType(row, group.First());
            rows.Add(row);
        }

        // Legacy lines 1186-1225 -- Credit entries.
        foreach (var group in GroupByEmployeeWageTypeAccountAndCostElements(
                     lines.Where(l => l.BillingType == BillingType.Deduction)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var row = NewPostingRow(rows, group.First(),
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0);
            StampEmployeeAndWageType(row, group.First());
            rows.Add(row);
        }

        // Legacy lines 1228-1286 -- Net Salary entries.
        foreach (var group in GroupByEmployeeWageTypeAccountAndCostElements(
                     lines.Where(l => l.WageTypeCode == NetPayWageTypeCode && l.Amount != 0)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var row = NewPostingRow(rows, group.First(),
                debit: amount < 0 ? -amount : 0,
                credit: amount >= 0 ? amount : 0);
            StampEmployeeAndWageType(row, group.First());
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Legacy lines 590-614. The combo does not name a payroll area: "0" means every area whose NAME is
    /// not "Sub-Contractors", "1" means only that one. A blank combo means no filter at all.
    /// </summary>
    private async Task<List<int>> ResolvePayrollAreaIdsAsync(PayrollJVCustom019PayrollAreaScope? scope)
    {
        if (!scope.HasValue)
        {
            return new List<int>();
        }

        var query = _payrollAreaRepository.GetAll();

        query = scope.Value == PayrollJVCustom019PayrollAreaScope.SubContractorsOnly
            ? query.Where(a => a.Name == SubContractorsPayrollAreaName)
            : query.Where(a => a.Name != SubContractorsPayrollAreaName);

        return await query.Select(a => a.Id).ToListAsync();
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

    private static PayrollJVCustom019ReportRowDto NewPostingRow(
        List<PayrollJVCustom019ReportRowDto> rows,
        PayrollJVPostingLine sample,
        double debit,
        double credit,
        string controlAcct = "")
    {
        return new PayrollJVCustom019ReportRowDto
        {
            Id = rows.Count + 1,
            LineNumber = rows.Count + 1,
            GLCode = sample.AccountInfoCode ?? string.Empty,
            GLName = sample.AccountInfoName ?? string.Empty,
            ControlAcct = controlAcct,
            Debit = debit,
            Credit = credit,
            RemarksTemplate = string.Empty,
            Project = sample.CostElement4Code ?? string.Empty,
            LineOfBusiness = sample.CostElement1Code ?? string.Empty,
            // LEGACY DEFECT REPRODUCED: legacy never writes Vehicle/Machinery on a posting branch, even
            // though it groups by CostElement2. Left blank on purpose -- see the class remarks.
            VehicleMachinery = string.Empty,
            Departments = sample.CostElement3Code ?? string.Empty
        };
    }

    private static void StampEmployeeAndWageType(
        PayrollJVCustom019ReportRowDto row,
        PayrollJVPostingLine sample)
    {
        row.EmployeeCode = sample.EmployeeCode;
        row.WageTypeName = sample.WageTypeName;
    }

    /// <summary>Legacy <c>GetTable()</c>: blank month/year mean "now"; an invalid pair fell back to 1900.</summary>
    private DateTime ResolveEndDate(GetPayrollJVCustom019ReportInput input)
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
    private string BuildJvParameterString(GetPayrollJVCustom019ReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaScope.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaScope.Value);
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
    private static List<PayrollJVReportColumnDto> GetColumnDtos(PayrollJVCustom019Format format)
    {
        switch (format)
        {
            // Legacy lines 1042-1076.
            case PayrollJVCustom019Format.MissingAccounts:
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

            // Legacy lines 1116-1140.
            case PayrollJVCustom019Format.EmployeeWiseBreakDown:
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
                    new("remarksTemplate", "RemarksTemplate"),
                    new("project", "Project"),
                    new("lineOfBusiness", "LineOfBusiness"),
                    new("vehicleMachinery", "VehicleMachinery"),
                    new("departments", "Departments")
                };

            // Legacy lines 806-822 (Main Format) and 953-969 (Provision Format) -- identical lists.
            default:
                return new List<PayrollJVReportColumnDto>
                {
                    new("lineNumber", "SrNo"),
                    new("glCode", "GLAcctBPCode"),
                    new("glName", "GLAcctBPName"),
                    new("controlAcct", "ControlAcct"),
                    new("debit", "Debit"),
                    new("credit", "Credit"),
                    new("remarksTemplate", "RemarksTemplate"),
                    new("project", "Project"),
                    new("lineOfBusiness", "LineOfBusiness"),
                    new("vehicleMachinery", "VehicleMachinery"),
                    new("departments", "Departments")
                };
        }
    }

    private static IReadOnlyList<GridReportColumn<PayrollJVCustom019ReportRowDto>> GetExportColumns(
        PayrollJVCustom019Format format)
    {
        switch (format)
        {
            case PayrollJVCustom019Format.MissingAccounts:
                return new List<GridReportColumn<PayrollJVCustom019ReportRowDto>>
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

            case PayrollJVCustom019Format.EmployeeWiseBreakDown:
                return new List<GridReportColumn<PayrollJVCustom019ReportRowDto>>
                {
                    new("SrNo", r => r.LineNumber),
                    new("EmpCode", r => r.EmployeeCode),
                    new("WageType", r => r.WageTypeName),
                    new("GLAcctBPCode", r => r.GLCode),
                    new("GLAcctBPName", r => r.GLName),
                    new("ControlAcct", r => r.ControlAcct),
                    new("Debit", r => r.Debit),
                    new("Credit", r => r.Credit),
                    new("RemarksTemplate", r => r.RemarksTemplate),
                    new("Project", r => r.Project),
                    new("LineOfBusiness", r => r.LineOfBusiness),
                    new("VehicleMachinery", r => r.VehicleMachinery),
                    new("Departments", r => r.Departments)
                };

            default:
                return new List<GridReportColumn<PayrollJVCustom019ReportRowDto>>
                {
                    new("SrNo", r => r.LineNumber),
                    new("GLAcctBPCode", r => r.GLCode),
                    new("GLAcctBPName", r => r.GLName),
                    new("ControlAcct", r => r.ControlAcct),
                    new("Debit", r => r.Debit),
                    new("Credit", r => r.Credit),
                    new("RemarksTemplate", r => r.RemarksTemplate),
                    new("Project", r => r.Project),
                    new("LineOfBusiness", r => r.LineOfBusiness),
                    new("VehicleMachinery", r => r.VehicleMachinery),
                    new("Departments", r => r.Departments)
                };
        }
    }
}
