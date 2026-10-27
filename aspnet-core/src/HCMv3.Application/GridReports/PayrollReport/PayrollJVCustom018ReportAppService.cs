using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 018) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom018.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 530-911).
///
/// SHAPE: unlike almost every other report in this wave, this one is NOT "one row per business record".
/// It emits a FIXED voucher of 13 general-ledger lines whose amounts are aggregates of the month's
/// salary register, bucketed by the employee's ORG-UNIT NAMES ("Medical Service", "Optical Shop",
/// "Cafeteria", "Business Support Services"/"Management") and by whether the employee is paid through a
/// bank. Because the rows are computed, the shared query method is <c>BuildRowsAsync</c> rather than an
/// <c>IQueryable</c> projection -- but it is still the ONE method both the paged read and the export
/// call, so the export can never honour different filters from the grid.
///
/// MODEL ADAPTATION: legacy read <c>EmployeeOrgAssignment.OrgLevel1..OrgLevel7</c> as seven discrete FKs
/// (legacy lines 610-620). HCMv3 collapses those into one hierarchical <c>OrganizationUnitId</c>, so the
/// same seven names come from <see cref="EmployeeOrgLevelNameResolver"/> -- the standing decision for
/// this wave -- and the bucket tests compare against the resolved names exactly as legacy did.
///
/// DIVERGENCE (agreed for the whole wave): legacy <c>Table1()</c> never called
/// <c>ApplyRowLevelSecurity</c> (the helper exists, unused, at line 1464). RLS IS applied here. A journal
/// voucher aggregates salary, so leaking it to a user restricted to a subset of employees is worse than
/// the inconsistency with legacy.
///
/// LEGACY DEFECT REPRODUCED: the Payroll Area combo on the panel is INERT. Legacy stores the selection in
/// <c>Session[payrollArea]</c> (line 43/274) and prints it into the execution-log parameter string
/// (<c>GetParameterStr</c>, line 507), but <c>Table1()</c> never reads it, so it filters nothing. Kept
/// verbatim and pinned by
/// <c>Should_Not_Filter_By_Payroll_Area_Because_Legacy_Never_Reads_The_Selection</c>.
///
/// ACCOUNTING REVIEW -- see the class-level remarks on <see cref="BuildRowsAsync"/>: this voucher does
/// NOT balance to zero, and two of its lines are an exact self-cancelling pair on the same account.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down/drill-up session stack
/// (lines 86-175), the ColumnChooser JSON persistence (lines 1423-1463), the callback-panel state machine
/// (lines 1275-1392), the PDF export button (line 1397) and the dead <c>Table1_Old()</c> (lines 912-1274).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom018)]
public class PayrollJVCustom018ReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom018ReportAppService
{
    /// <summary>Legacy lines 560-564: Annual Leave Pay / Arrear, Leave Encashment Pay / Arrear.</summary>
    private static readonly string[] AnnualLeaveWageTypeCodes = { "1003", "7003", "1052", "7052" };

    /// <summary>Legacy line 565: Annual Leave Provision.</summary>
    private static readonly string[] LeaveProvisionWageTypeCodes = { "3001" };

    /// <summary>Legacy line 567: Indemnity Provision.</summary>
    private static readonly string[] IndemnityProvisionWageTypeCodes = { "3000" };

    /// <summary>Legacy line 569: Net Pay.</summary>
    private static readonly string[] NetWageTypeCodes = { "9000" };

    /// <summary>Legacy line 620: org-unit names that make an employee "Clinic".</summary>
    private static readonly string[] ClinicOrgNames = { "Medical Service" };

    /// <summary>Legacy line 629: org-unit names that make an employee "Admin".</summary>
    private static readonly string[] AdminOrgNames = { "Business Support Services", "Management" };

    /// <summary>Legacy line 638: org-unit names that make an employee "Optical".</summary>
    private static readonly string[] OpticalOrgNames = { "Optical Shop" };

    /// <summary>Legacy line 647: org-unit names that make an employee "Cafeteria".</summary>
    private static readonly string[] CafeteriaOrgNames = { "Cafeteria" };

    /// <summary>Legacy line 674: the Description suffix format.</summary>
    private const string MonthYearFormat = "MMMM yyyy";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PayrollJVCustom018ReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<Employee> employeeRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _employeeBankRepository = employeeBankRepository;
        _employeeRepository = employeeRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom018";

    protected override string ScreenCaption => "Payroll JV (Custom 018)";

    [HttpPost]
    public async Task<PayrollJVCustom018ReportOutput> GetPayrollJVCustom018Report(
        GetPayrollJVCustom018ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildJvParameterString(input),
            rows.Count);

        return new PayrollJVCustom018ReportOutput
        {
            TotalCount = rows.Count,
            Items = paged,
            Columns = GetColumnDtos()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom018ReportToExcel(GetPayrollJVCustom018ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom018.xlsx",
            ordered,
            GetExportColumns(),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy <c>Table1()</c> (lines 530-911) end to end.
    ///
    /// ACCOUNTING REVIEW -- FLAGGED, NOT FIXED. Two findings, both reproduced verbatim because legacy
    /// fidelity is the standing rule for this wave, and both pinned by tests:
    ///
    /// 1. THE VOUCHER DOES NOT BALANCE. The four salary-expense debits (60001 Clinic, 60011 Admin,
    ///    60037 Opticals, 60036 Icafe) have no offsetting credit: the three 15001 lines cancel each other
    ///    out (see 2) and the 15017 Leave Salary line credits annual leave for ALL employees while the
    ///    expense lines subtract annual leave only for employees inside the four org buckets. The
    ///    voucher total therefore equals
    ///    <c>net(buckets) - annualLeave(buckets) + annualLeave(all)</c>, which is zero only if the four
    ///    buckets happen to partition the workforce.
    /// 2. THE 15001 LINES SELF-CANCEL. "KSEC Salary" is <c>-net(bank-paid)</c> and "Paid Salary" is
    ///    <c>+net(bank-paid)</c>; "Cash KSEC Salary" is <c>-net(cash-paid)</c> and "Cash Salary" is
    ///    <c>+net(cash-paid)</c>. All four post to account 15001, so the account nets to zero on every
    ///    run. Either the target system consumes these as four separate transactions, or one of each pair
    ///    was meant to carry a different GL account. This needs a customer decision before the report is
    ///    used to post.
    /// </summary>
    private async Task<List<PayrollJVCustom018ReportRowDto>> BuildRowsAsync(
        GetPayrollJVCustom018ReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var payrollRunType = input.PayrollRunType;

        // Legacy lines 571-578 + 580-586.
        var wageTypeCodes = AnnualLeaveWageTypeCodes
            .Concat(LeaveProvisionWageTypeCodes)
            .Concat(IndemnityProvisionWageTypeCodes)
            .Concat(NetWageTypeCodes)
            .ToArray();

        var details = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && wageTypeCodes.Contains(d.WageType.Code))
            .WhereIf(payrollRunType.HasValue,
                d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            // DIVERGENCE: legacy never applied RLS here. An empty list still means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new SalaryLine
            {
                EmployeeId = d.SalaryRegister.EmployeeId,
                WageTypeCode = d.WageType.Code,
                Amount = d.Amount
            })
            .ToListAsync();

        // Legacy lines 604-618: the org assignment current as of the month end, with its seven level
        // names -- here resolved from the org-unit tree (see class remarks).
        var orgLevelLookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= endDate && o.EndDate >= endDate)
            .WhereIf(rlsEmployeeIds.Count > 0, o => rlsEmployeeIds.Contains(o.EmployeeId))
            .Select(o => new { o.EmployeeId, o.OrganizationUnitId })
            .ToListAsync();

        var employeeOrgNames = assignments
            .Select(a => new { a.EmployeeId, Names = orgLevelLookup.GetLevelNames(a.OrganizationUnitId) })
            .ToList();

        var clinicEmployeeIds = BucketEmployeeIds(employeeOrgNames.Select(a => (a.EmployeeId, a.Names)), ClinicOrgNames);
        var adminEmployeeIds = BucketEmployeeIds(employeeOrgNames.Select(a => (a.EmployeeId, a.Names)), AdminOrgNames);
        var opticalEmployeeIds = BucketEmployeeIds(employeeOrgNames.Select(a => (a.EmployeeId, a.Names)), OpticalOrgNames);
        var cafeteriaEmployeeIds = BucketEmployeeIds(employeeOrgNames.Select(a => (a.EmployeeId, a.Names)), CafeteriaOrgNames);

        // Legacy lines 661-672: bank-paid = has a BANK EmployeeBank row covering the month end;
        // cash-paid = every other employee (legacy queried the whole Employee table).
        var bankEmployeeIds = (await _employeeBankRepository.GetAll()
            .Where(b => b.StartDate <= endDate && b.EndDate >= endDate && b.PaymentType == PaymentType.Bank)
            .WhereIf(rlsEmployeeIds.Count > 0, b => rlsEmployeeIds.Contains(b.EmployeeId))
            .Select(b => b.EmployeeId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var cashEmployeeIds = (await _employeeRepository.GetAll()
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync())
            .Where(id => !bankEmployeeIds.Contains(id))
            .ToHashSet();

        var monthYear = endDate.ToString(MonthYearFormat, CultureInfo.InvariantCulture);

        double SumBy(Func<SalaryLine, bool> predicate)
        {
            return details.Where(predicate).Sum(d => d.Amount);
        }

        bool IsNet(SalaryLine d) => NetWageTypeCodes.Contains(d.WageTypeCode);
        bool IsAnnualLeave(SalaryLine d) => AnnualLeaveWageTypeCodes.Contains(d.WageTypeCode);
        bool IsLeaveProvision(SalaryLine d) => LeaveProvisionWageTypeCodes.Contains(d.WageTypeCode);
        bool IsIndemnityProvision(SalaryLine d) => IndemnityProvisionWageTypeCodes.Contains(d.WageTypeCode);

        var rows = new List<PayrollJVCustom018ReportRowDto>();

        void AddRow(string glAccount, string caption, double amount)
        {
            rows.Add(new PayrollJVCustom018ReportRowDto
            {
                Id = rows.Count + 1,
                LineNumber = rows.Count + 1,
                Date = endDate,
                Reference = string.Empty,
                GLAccount = glAccount,
                Description = caption + " " + monthYear,
                Amount = amount,
                JobId = string.Empty,
                UsedForReimbursableExpenses = "FALSE",
                TransactionPeriod = string.Empty,
                TransactionNumber = string.Empty,
                ConsolidatedTransaction = "FALSE"
            });
        }

        // Legacy lines 676-693 -- "Salary Clinic": clinic employees who are NOT also optical.
        AddRow("60001", "Salary Clinic",
            SumBy(d => clinicEmployeeIds.Contains(d.EmployeeId) && !opticalEmployeeIds.Contains(d.EmployeeId) && IsNet(d))
            - SumBy(d => clinicEmployeeIds.Contains(d.EmployeeId) && !opticalEmployeeIds.Contains(d.EmployeeId) && IsAnnualLeave(d)));

        // Legacy lines 695-712 -- "Salary Admin": admin employees who are NOT also cafeteria.
        AddRow("60011", "Salary Admin",
            SumBy(d => adminEmployeeIds.Contains(d.EmployeeId) && !cafeteriaEmployeeIds.Contains(d.EmployeeId) && IsNet(d))
            - SumBy(d => adminEmployeeIds.Contains(d.EmployeeId) && !cafeteriaEmployeeIds.Contains(d.EmployeeId) && IsAnnualLeave(d)));

        // Legacy lines 714-729 -- "Salary Opticals".
        AddRow("60037", "Salary Opticals",
            SumBy(d => opticalEmployeeIds.Contains(d.EmployeeId) && IsNet(d))
            - SumBy(d => opticalEmployeeIds.Contains(d.EmployeeId) && IsAnnualLeave(d)));

        // Legacy lines 731-746 -- "Salary Icafe".
        AddRow("60036", "Salary Icafe",
            SumBy(d => cafeteriaEmployeeIds.Contains(d.EmployeeId) && IsNet(d))
            - SumBy(d => cafeteriaEmployeeIds.Contains(d.EmployeeId) && IsAnnualLeave(d)));

        // Legacy lines 748-761 -- "Leave Salary": annual leave for EVERY employee, not just the buckets.
        AddRow("15017", "Leave Salary", SumBy(IsAnnualLeave));

        // Legacy lines 763-777 -- "KSEC Salary".
        AddRow("15001", "KSEC Salary",
            -1 * SumBy(d => bankEmployeeIds.Contains(d.EmployeeId) && IsNet(d)));

        // Legacy lines 779-793 -- "Cash KSEC Salary".
        AddRow("15001", "Cash KSEC Salary",
            -1 * SumBy(d => cashEmployeeIds.Contains(d.EmployeeId) && IsNet(d)));

        // Legacy lines 795-809 -- "Paid Salary" (exact negation of "KSEC Salary"; see the flag above).
        AddRow("15001", "Paid Salary",
            SumBy(d => bankEmployeeIds.Contains(d.EmployeeId) && IsNet(d)));

        // Legacy lines 811-825 -- "Cash Salary" (exact negation of "Cash KSEC Salary").
        AddRow("15001", "Cash Salary",
            SumBy(d => cashEmployeeIds.Contains(d.EmployeeId) && IsNet(d)));

        // Legacy lines 827-840 -- "Leave Pay Exp" debit / lines 842-856 credit.
        AddRow("60007", "Leave Pay Exp", SumBy(IsLeaveProvision));
        AddRow("15017", "Leave Pay Exp", -1 * SumBy(IsLeaveProvision));

        // Legacy lines 858-871 -- "Indemnity Pay Exp" debit / lines 873-887 credit.
        AddRow("60008", "Indemnity Pay Exp", SumBy(IsIndemnityProvision));
        AddRow("15018", "Indemnity Pay Exp", -1 * SumBy(IsIndemnityProvision));

        return rows;
    }

    /// <summary>
    /// Legacy lines 620-627 etc.: an employee belongs to a bucket when ANY of its seven OrgLevel names
    /// is in the bucket's name list.
    /// </summary>
    private static HashSet<int> BucketEmployeeIds(
        IEnumerable<(int EmployeeId, string[] Names)> assignments,
        string[] bucketNames)
    {
        return assignments
            .Where(a => a.Names.Any(bucketNames.Contains))
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToHashSet();
    }

    /// <summary>
    /// Legacy <c>GetTable()</c> lines 450-468: blank month/year mean "now", and an unparseable
    /// month/year fell back to January 1900 rather than throwing. Reproduced, including the fallback.
    /// </summary>
    private DateTime ResolveEndDate(GetPayrollJVCustom018ReportInput input)
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

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (lines 502-524): Payroll Area | Run Type | Month | Year. Note the
    /// Payroll Area is logged even though it filters nothing -- see the class remarks.
    /// </summary>
    private string BuildJvParameterString(GetPayrollJVCustom018ReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            // Legacy logged the combo's caption; only the id crosses the wire and identifies the same
            // selection.
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The legacy column list (lines 542-561) in legacy order, as localization keys. The same list drives
    /// the Angular grid and <see cref="GetExportColumns"/>, so the two cannot drift.
    /// </summary>
    private static List<PayrollJVReportColumnDto> GetColumnDtos()
    {
        return new List<PayrollJVReportColumnDto>
        {
            new("date", "Date"),
            new("reference", "Reference"),
            new("glAccount", "GLAccount"),
            new("description", "Description"),
            new("amount", "Amount"),
            new("jobId", "JobID"),
            new("usedForReimbursableExpenses", "UsedForReimbursableExpenses"),
            new("transactionPeriod", "TransactionPeriod"),
            new("transactionNumber", "TransactionNumber"),
            new("consolidatedTransaction", "ConsolidatedTransaction")
        };
    }

    private static IReadOnlyList<GridReportColumn<PayrollJVCustom018ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PayrollJVCustom018ReportRowDto>>
        {
            new("Date", r => r.Date),
            new("Reference", r => r.Reference),
            new("GLAccount", r => r.GLAccount),
            new("Description", r => r.Description),
            new("Amount", r => r.Amount),
            new("JobID", r => r.JobId),
            new("UsedForReimbursableExpenses", r => r.UsedForReimbursableExpenses),
            new("TransactionPeriod", r => r.TransactionPeriod),
            new("TransactionNumber", r => r.TransactionNumber),
            new("ConsolidatedTransaction", r => r.ConsolidatedTransaction)
        };
    }

    /// <summary>Flattened salary-register line, mirroring legacy's anonymous projection (lines 588-602).</summary>
    private sealed class SalaryLine
    {
        public int EmployeeId { get; set; }

        public string WageTypeCode { get; set; }

        public double Amount { get; set; }
    }
}
