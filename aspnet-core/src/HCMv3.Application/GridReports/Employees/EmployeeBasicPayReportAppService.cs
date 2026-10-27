using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Basic Pay analysis grid report -- port of legacy
/// <c>uscRptEmployeeBasicPay.ascx(.cs)</c>, method <c>Table1()</c> (lines 462-706).
///
/// ATYPICAL on three counts -- DO NOT COPY ANY OF THEM INTO A PLAIN REPORT:
/// <list type="number">
/// <item><description>NO DATE FILTER. The legacy panel shows dtFrom/dtTo but <c>Table1()</c> never
/// reads them (the date predicate is commented out at lines 552-554) and <c>GetParameterStr()</c> does
/// not log them. Reproduced: the dates are ignored here too.</description></item>
/// <item><description>DYNAMIC COLUMNS. Legacy generated one grid column per wage type present in the
/// data (lines 481, 540-545). They are carried as the <c>WageAmounts</c> list on each row and expanded
/// into columns at export time, which is why <see cref="GetExportColumns"/> takes the rows.</description></item>
/// <item><description>NO DATABASE PAGING. The "Increment Amount" column (lines 680-701) is a
/// difference against the employee's previous pay record, so it can only be computed once the whole
/// filtered set is in hand. <see cref="BuildRowsAsync"/> therefore materialises everything and the
/// paged read sorts/pages in memory. The read and the export still share that one builder, so the
/// export cannot drift from the grid's filters.</description></item>
/// </list>
///
/// A SECOND LEVEL exists in legacy: <c>Table2()</c> (lines 912-988) is the drill-down showing the wage
/// lines of the selected <c>EmployeeBasicPay</c> (OID, Code, Name, StartDate, EndDate, WageType, Amount,
/// Currency). It is not a separate endpoint here because every one of those values is already on the
/// level-1 row via <c>WageAmounts</c>; the Angular screen renders it as an expandable row.
///
/// NOT PORTED (DevExpress/WebForms UI plumbing): the drill-up/drill-down session stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button and the dead
/// <c>Table1_Old()</c> (lines 707+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeBasicPay)]
public class EmployeeBasicPayReportAppService : GridReportAppServiceBase, IEmployeeBasicPayReportAppService
{
    /// <summary>Legacy line 626 excludes this wage type (basic pay) from the allowance total.</summary>
    private const string BasicPayWageTypeCode = "1000";

    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeBasicPayReportAppService(
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeBasicPay";

    protected override string ScreenCaption => "Employee Basic Pay";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeBasicPayReportRowDto>> GetEmployeeBasicPayReport(
        GetEmployeeBasicPayReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var totalCount = rows.Count;

        var pagedRows = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildBasicPayParameterString(input), totalCount);

        return new PagedResultDto<EmployeeBasicPayReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeBasicPayReportToExcel(GetEmployeeBasicPayReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var orderedRows = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "EmployeeBasicPay.xlsx",
            orderedRows,
            GetExportColumns(orderedRows),
            BuildBasicPayParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (uscRptEmployeeBasicPay.ascx.cs:444-454) logs ONLY the employee
    /// status and the employee code -- this report has no working date filter. The shared base helper
    /// always appends the two dates, so the audit string carries them even though they filter nothing.
    /// </summary>
    private string BuildBasicPayParameterString(GetEmployeeBasicPayReportInput input)
    {
        return BuildParameterString(input);
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy <c>Table1()</c> (uscRptEmployeeBasicPay.ascx.cs:462-706).
    /// </summary>
    private async Task<List<EmployeeBasicPayReportRowDto>> BuildRowsAsync(GetEmployeeBasicPayReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Legacy line 574: the org-assignment lookup is keyed on DateTime.Now. The same "today" also
        // decides the Current/History status column (line 662).
        var today = Today;

        var query = _employeeBasicPayRepository.GetAll()
            // Legacy line 551: no date predicate at all -- the from/to pickers are dead UI here.
            // Legacy lines 556-561: Employee.Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, p => p.Employee.Status == employeeStatus.Value)
            // Legacy lines 563-568: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, p => employeeCodes.Contains(p.Employee.Code))
            // Legacy line 570 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, p => rlsEmployeeIds.Contains(p.EmployeeId));

        var rows = await query.Select(p => new EmployeeBasicPayReportRowDto
        {
            Id = p.Id,
            EmployeeCode = p.Employee.Code,
            EmployeeName = useArabicNames ? p.Employee.FullNameAr : p.Employee.FullNameEn,
            JoiningDate = p.Employee.JoiningDate,
            EmployeeStatusName = p.Employee.Status.ToString(),
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            // Legacy lines 570-586 + 640-650: JobTitle / WorkLocation / OrgLevel1..7 all come from the
            // SAME org assignment -- the one current TODAY, not as of any report date.
            JobTitleName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == p.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            WorkLocationName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == p.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == p.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            // Legacy line 662.
            RecordStatus = p.StartDate <= today && p.EndDate >= today ? "Current" : "History",
            // Legacy line 627: the record's own Amount, NOT a sum of its lines.
            TotalAmount = p.Amount
        }).ToListAsync();

        if (rows.Count == 0)
        {
            return rows;
        }

        // HCMv3's EmployeeBasicPay has no BasicPayDetail collection navigation (legacy XPO did), so the
        // wage lines are fetched in one extra query and attached in memory.
        var basicPayIds = rows.Select(r => r.Id).ToList();

        var details = await _basicPayDetailRepository.GetAll()
            .Where(d => basicPayIds.Contains(d.EmployeeBasicPayId))
            .Select(d => new WageLine
            {
                EmployeeBasicPayId = d.EmployeeBasicPayId,
                WageTypeId = d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = useArabicNames ? d.WageType.NameAr : d.WageType.NameEn,
                PayrollOrder = d.WageType.PayrollOrder,
                IncludeInBasicPay = d.WageType.IncludeInBasicPay,
                Amount = d.Amount,
                CurrencyShortForm = d.Currency == null
                    ? string.Empty
                    : (useArabicNames ? d.Currency.CurrencyShortFormAr : d.Currency.CurrencyShortFormEn)
            })
            .ToListAsync();

        var detailsByPayId = details.GroupBy(d => d.EmployeeBasicPayId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var row in rows)
        {
            var lines = detailsByPayId.TryGetValue(row.Id, out var found) ? found : new List<WageLine>();

            // Legacy ordered the generated wage columns by PayrollOrder (line 481).
            row.WageAmounts = lines
                .OrderBy(w => w.PayrollOrder)
                .ThenBy(w => w.WageTypeCode)
                .Select(w => new EmployeeBasicPayReportWageAmountDto
                {
                    WageTypeId = w.WageTypeId,
                    WageTypeCode = w.WageTypeCode,
                    WageTypeName = w.WageTypeName,
                    PayrollOrder = w.PayrollOrder,
                    Amount = w.Amount,
                    CurrencyShortForm = w.CurrencyShortForm
                })
                .ToList();

            // Legacy line 626: all wage lines except basic pay ("1000"), and only those flagged
            // IncludeInBasicPay.
            row.TotalAllowance = lines
                .Where(w => w.WageTypeCode != BasicPayWageTypeCode && w.IncludeInBasicPay)
                .Sum(w => w.Amount);

            // Legacy lines 655-663: the currency of whichever wage line came first.
            row.Currency = row.WageAmounts.Select(w => w.CurrencyShortForm).FirstOrDefault() ?? string.Empty;
        }

        await StampOrgLevelsAsync(rows);

        ApplyIncrementAmounts(rows);

        return rows;
    }

    /// <summary>
    /// Ports legacy's "Calculate Difference" block (uscRptEmployeeBasicPay.ascx.cs:680-701): group by
    /// employee code, order the group by StartDate DESCENDING, and set each row's increment to its
    /// TotalAmount minus the following row's. Legacy leaves the increment at 0 when the previous amount
    /// is 0 (so the oldest record in a group is always 0), which is reproduced here.
    /// </summary>
    private static void ApplyIncrementAmounts(IEnumerable<EmployeeBasicPayReportRowDto> rows)
    {
        foreach (var group in rows.GroupBy(r => r.EmployeeCode))
        {
            var ordered = group.OrderByDescending(r => r.StartDate).ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var currentAmount = ordered[i].TotalAmount;
                var previousAmount = i < ordered.Count - 1 ? ordered[i + 1].TotalAmount : 0;

                ordered[i].IncrementAmount = previousAmount != 0 ? currentAmount - previousAmount : 0;
            }
        }
    }

    /// <summary>
    /// The exported column set, in legacy column order, with the dynamic per-wage-type columns spliced
    /// in between "Currency" and "TotalAllowance" exactly where legacy put them (lines 540-545). The
    /// wage-type column list is the union across the exported rows, ordered by PayrollOrder, which is
    /// what legacy derived from the data too.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeBasicPayReportRowDto>> GetExportColumns(
        IReadOnlyCollection<EmployeeBasicPayReportRowDto> rows)
    {
        var columns = new List<GridReportColumn<EmployeeBasicPayReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JoiningDate", r => r.JoiningDate),
            new("EmployeeStatus", r => r.EmployeeStatusName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("JobTitle", r => r.JobTitleName),
            new("WorkLocation", r => r.WorkLocationName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Status", r => r.RecordStatus),
            new("Currency", r => r.Currency)
        };

        var wageTypes = rows
            .SelectMany(r => r.WageAmounts)
            .GroupBy(w => w.WageTypeId)
            .Select(g => g.First())
            .OrderBy(w => w.PayrollOrder)
            .ThenBy(w => w.WageTypeCode)
            .ToList();

        foreach (var wageType in wageTypes)
        {
            var wageTypeId = wageType.WageTypeId;

            // The header is the wage type's own name, not a localization key -- legacy named these
            // columns after the wage type as well. FromLiteralHeader keeps ABP from rendering it as
            // "[Basic Salary]" via its not-found fallback.
            columns.Add(GridReportColumn<EmployeeBasicPayReportRowDto>.FromLiteralHeader(
                wageType.WageTypeName,
                r => r.WageAmounts.Where(w => w.WageTypeId == wageTypeId).Select(w => (object)w.Amount).FirstOrDefault()));
        }

        columns.Add(new GridReportColumn<EmployeeBasicPayReportRowDto>("TotalAllowance", r => r.TotalAllowance));
        columns.Add(new GridReportColumn<EmployeeBasicPayReportRowDto>("TotalAmount", r => r.TotalAmount));
        columns.Add(new GridReportColumn<EmployeeBasicPayReportRowDto>("IncrementAmount", r => r.IncrementAmount));

        return columns;
    }

    /// <summary>Flat carrier for one <c>EmployeeBasicPayDetail</c> row while it is being attached.</summary>
    private sealed class WageLine
    {
        public int EmployeeBasicPayId { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeName { get; set; }

        public int PayrollOrder { get; set; }

        public bool IncludeInBasicPay { get; set; }

        public double Amount { get; set; }

        public string CurrencyShortForm { get; set; }
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeBasicPayReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }
}
