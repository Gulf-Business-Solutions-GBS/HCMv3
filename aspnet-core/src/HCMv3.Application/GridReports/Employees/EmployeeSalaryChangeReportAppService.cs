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
/// Employee Salary Change analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeSalaryChange.ascx(.cs)</c>, whose
/// entire data contract is <c>Table1()</c> (lines 436-600) plus its helper <c>GetComponents()</c>.
///
/// ATYPICAL FOR THIS WAVE: it is a PIVOT. Legacy added three DataTable columns per wage type found in
/// <c>EmployeeBasicPayDetail</c> ("Old X" / "Adj X" / "Final X", lines 483-500) and compared, per
/// employee, the last basic pay STARTING BEFORE the From date against the last one STARTING ON OR BEFORE
/// the To date. Because the columns are data-driven they cannot be fixed DTO properties -- they are
/// carried as <c>Components</c> on every row, and the Excel exporter builds its dynamic headers from the
/// same list.
///
/// DELIBERATE DEVIATION -- ROW LEVEL SECURITY: legacy's <c>Table1()</c> here NEVER calls
/// <c>ApplyRowLevelSecurity</c>, unlike every sibling report in the folder, so any user who could open the
/// screen saw every employee's salary. That is a security defect, not a contract: RLS is applied here.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (single "Table1"), the ColumnChooser JSON persistence, the callback-panel state machine and the
/// PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeSalaryChange)]
public class EmployeeSalaryChangeReportAppService : GridReportAppServiceBase, IEmployeeSalaryChangeReportAppService
{
    private const string BasicPayWageTypeCode = "1000";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeSalaryChangeReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeSalaryChange";

    protected override string ScreenCaption => "Employee Salary Change";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSalaryChangeReportRowDto>> GetEmployeeSalaryChangeReport(
        GetEmployeeSalaryChangeReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await FillPivotAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeSalaryChangeReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeSalaryChangeReportToExcel(GetEmployeeSalaryChangeReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        var components = await FillPivotAsync(rows);

        return await ExportToFileAsync(
            "EmployeeSalaryChange.xlsx",
            rows,
            GetExportColumns(components),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND the scalar part of the projection, shared by the paged
    /// read and the export. Ports the row selection of legacy <c>Table1()</c>
    /// (uscRptEmployeeSalaryChange.ascx.cs:436-600); the wage-type pivot block is stamped on afterwards by
    /// <see cref="FillPivotAsync"/>, which BOTH callers also run.
    /// </summary>
    private async Task<IQueryable<EmployeeSalaryChangeReportRowDto>> BuildQueryAsync(
        GetEmployeeSalaryChangeReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;

        // Legacy line 507: the org block comes from the assignment current as of DateTime.Now.
        var today = Today;

        var basicPays = _employeeBasicPayRepository.GetAll();

        var query = _employeeRepository.GetAll()
            // Legacy line 524: the row set is every employee that has ANY basic pay record
            // (GetObjectsQuery<EmployeeBasicPay>().Select(x => x.Employee).Distinct()).
            .Where(e => basicPays.Any(p => p.EmployeeId == e.Id))
            // Not in legacy (the screen has no such controls); honoured when a caller sends them.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // NOT in legacy either -- see the class remarks on the RLS deviation.
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        return query.Select(e => new EmployeeSalaryChangeReportRowDto
        {
            // Legacy "OID" column -- the row key, not a data column.
            Id = e.Id,
            EmployeeCode = e.Code,
            // Legacy line 526 uses FullNameEn unconditionally on this screen.
            EmployeeName = e.FullNameEn,
            Status = e.Status,
            ParameterStartDate = fromDate,
            ParameterEndDate = toDate,
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == e.Id
                            && o.StartDate <= today
                            && o.EndDate >= today
                            && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            // Legacy lines 544-549 ("BEFORE"): the latest pay STARTING STRICTLY BEFORE the From date.
            OldBasicPayId = basicPays
                .Where(p => p.EmployeeId == e.Id && p.StartDate < fromDate)
                .OrderByDescending(p => p.StartDate).ThenByDescending(p => p.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefault(),
            // Legacy lines 552-556 ("AFTER"): the latest pay STARTING ON OR BEFORE the To date.
            FinalBasicPayId = basicPays
                .Where(p => p.EmployeeId == e.Id && p.StartDate <= toDate)
                .OrderByDescending(p => p.StartDate).ThenByDescending(p => p.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Stamps the dynamic wage-type pivot onto already-materialized rows and returns the column list, so
    /// the export can build matching headers. Called by BOTH the read and the export -- that is what keeps
    /// the exported sheet identical to the grid.
    ///
    /// Ports legacy <c>GetComponents()</c> (lines 602-625) plus the per-component loop (lines 563-585),
    /// including its "missing component reads as 0" rule.
    /// </summary>
    private async Task<List<EmployeeSalaryChangeComponentDto>> FillPivotAsync(
        List<EmployeeSalaryChangeReportRowDto> rows)
    {
        var componentColumns = await GetComponentColumnsAsync();

        var payIds = rows.Where(r => r.OldBasicPayId.HasValue).Select(r => r.OldBasicPayId.Value)
            .Concat(rows.Where(r => r.FinalBasicPayId.HasValue).Select(r => r.FinalBasicPayId.Value))
            .Distinct()
            .ToList();

        // One query for every detail row both compared pays need, instead of two per employee.
        var details = payIds.Count == 0
            ? new List<PayComponentAmount>()
            : (await _employeeBasicPayDetailRepository.GetAll()
                .Where(d => payIds.Contains(d.EmployeeBasicPayId))
                .GroupBy(d => new { d.EmployeeBasicPayId, d.WageTypeId })
                .Select(g => new
                {
                    g.Key.EmployeeBasicPayId,
                    g.Key.WageTypeId,
                    Amount = g.Sum(d => d.Amount)
                })
                .ToListAsync())
                .Select(g => new PayComponentAmount
                {
                    EmployeeBasicPayId = g.EmployeeBasicPayId,
                    WageTypeId = g.WageTypeId,
                    Amount = g.Amount
                })
                .ToList();

        var amountsByPay = details
            .GroupBy(d => d.EmployeeBasicPayId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(d => d.WageTypeId, d => d.Amount));

        foreach (var row in rows)
        {
            var oldAmounts = GetAmounts(amountsByPay, row.OldBasicPayId);
            var finalAmounts = GetAmounts(amountsByPay, row.FinalBasicPayId);

            row.Components = new List<EmployeeSalaryChangeComponentDto>(componentColumns.Count);
            row.OldTotal = 0;
            row.FinalTotal = 0;
            row.AdjustmentTotal = 0;

            foreach (var column in componentColumns)
            {
                // Legacy lines 567-568: a component absent from a pay counts as 0, never as "unknown".
                var oldValue = oldAmounts.TryGetValue(column.WageTypeId, out var o) ? o : 0;
                var finalValue = finalAmounts.TryGetValue(column.WageTypeId, out var f) ? f : 0;
                var adjustment = finalValue - oldValue;

                row.Components.Add(new EmployeeSalaryChangeComponentDto
                {
                    WageTypeCode = column.WageTypeCode,
                    WageTypeName = column.WageTypeName,
                    OldAmount = oldValue,
                    FinalAmount = finalValue,
                    AdjustmentAmount = adjustment
                });

                row.OldTotal += oldValue;
                row.FinalTotal += finalValue;
                row.AdjustmentTotal += adjustment;
            }
        }

        return componentColumns
            .Select(c => new EmployeeSalaryChangeComponentDto
            {
                WageTypeCode = c.WageTypeCode,
                WageTypeName = c.WageTypeName
            })
            .ToList();
    }

    private static Dictionary<int, double> GetAmounts(
        Dictionary<int, Dictionary<int, double>> amountsByPay,
        int? basicPayId)
    {
        if (basicPayId.HasValue && amountsByPay.TryGetValue(basicPayId.Value, out var amounts))
        {
            return amounts;
        }

        return new Dictionary<int, double>();
    }

    /// <summary>
    /// The pivot's column list: every wage type that appears anywhere in <c>EmployeeBasicPayDetail</c>,
    /// with the basic-pay wage type (code 1000) forced first and the rest by code -- legacy lines 472-481.
    /// </summary>
    private async Task<List<ComponentColumn>> GetComponentColumnsAsync()
    {
        // Projected to an anonymous type before Distinct: EF Core only translates Distinct over
        // anonymous/primitive projections, not over a named class.
        var columns = await _employeeBasicPayDetailRepository.GetAll()
            .Select(d => new
            {
                d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn
            })
            .Distinct()
            .ToListAsync();

        return columns
            .Select(c => new ComponentColumn
            {
                WageTypeId = c.WageTypeId,
                WageTypeCode = c.WageTypeCode,
                WageTypeName = c.WageTypeName
            })
            .OrderBy(c => c.WageTypeCode == BasicPayWageTypeCode ? 0 : 1)
            .ThenBy(c => c.WageTypeCode)
            .ToList();
    }

    /// <summary>
    /// The exported column set: the fixed block in legacy column order, then Old/Adj/Final blocks each
    /// followed by their total -- legacy lines 483-500. The wage-type headers are data, not localization
    /// keys, so they are passed as literal headers.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeSalaryChangeReportRowDto>> GetExportColumns(
        List<EmployeeSalaryChangeComponentDto> components)
    {
        var columns = new List<GridReportColumn<EmployeeSalaryChangeReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Status", r => r.Status.ToString()),
            new("ParameterStartDate", r => r.ParameterStartDate),
            new("ParameterEndDate", r => r.ParameterEndDate)
        };

        for (var i = 0; i < components.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<EmployeeSalaryChangeReportRowDto>.FromLiteralHeader(
                "Old " + components[index].WageTypeName,
                r => r.Components[index].OldAmount));
        }

        columns.Add(new("OldTotal", r => r.OldTotal));

        for (var i = 0; i < components.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<EmployeeSalaryChangeReportRowDto>.FromLiteralHeader(
                "Adj " + components[index].WageTypeName,
                r => r.Components[index].AdjustmentAmount));
        }

        columns.Add(new("AdjustmentTotal", r => r.AdjustmentTotal));

        for (var i = 0; i < components.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<EmployeeSalaryChangeReportRowDto>.FromLiteralHeader(
                "Final " + components[index].WageTypeName,
                r => r.Components[index].FinalAmount));
        }

        columns.Add(new("FinalTotal", r => r.FinalTotal));

        return columns;
    }

    private class ComponentColumn
    {
        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeName { get; set; }
    }

    private class PayComponentAmount
    {
        public int EmployeeBasicPayId { get; set; }

        public int WageTypeId { get; set; }

        public double Amount { get; set; }
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeSalaryChangeReportRowDto> rows)
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
