using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
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
/// Salary By Nationality analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptSalaryByNationality.ascx(.cs)</c>.
///
/// Ports <c>Table1()</c> (lines 499-598). Unlike the reference report this one is an AGGREGATE: it
/// groups the <see cref="EmployeeBasicPay"/> rows that are current TODAY by the employee's nationality
/// and emits one row per nationality. It still fits the two-operation grid-report shape because the
/// grouped result is itself a flat, sortable, pageable list.
///
/// TWO ORDERING SUBTLETIES OF THE LEGACY QUERY, both reproduced deliberately:
/// 1. <c>TotalAmount</c> (line 531) is evaluated BEFORE the status/nationality/RLS filters, so
///    percentages of a filtered result do not sum to 100.
/// 2. The base predicate already excludes employees with no nationality (line 529), so those rows never
///    reach the grouping.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing: the drill-down "Table2" branch (commented out in
/// legacy's own <c>GetTable()</c>, lines 452-455, so it is dead in production), the ColumnChooser JSON
/// persistence, the callback-panel state machine, the PDF export button, and the dead
/// <c>Table1_Old()</c> (line 601+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SalaryByNationality)]
public class SalaryByNationalityReportAppService : GridReportAppServiceBase,
    ISalaryByNationalityReportAppService
{
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;

    public SalaryByNationalityReportAppService(IRepository<EmployeeBasicPay> employeeBasicPayRepository)
    {
        _employeeBasicPayRepository = employeeBasicPayRepository;
    }

    protected override string LegacyScreenName => "uscRptSalaryByNationality";

    protected override string ScreenCaption => "Salary By Nationality";

    [HttpPost]
    public async Task<PagedResultDto<SalaryByNationalityReportRowDto>> GetSalaryByNationalityReport(
        GetSalaryByNationalityReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterStringForReport(input), totalCount);

        return new PagedResultDto<SalaryByNationalityReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetSalaryByNationalityReportToExcel(GetSalaryByNationalityReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "SalaryByNationality.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterStringForReport(input));
    }

    /// <summary>
    /// Legacy logged ONLY the status and nationality filters for this report -- no dates
    /// (uscRptSalaryByNationality.ascx.cs:483-491), because it has no date inputs.
    /// </summary>
    private static string BuildParameterStringForReport(GetSalaryByNationalityReportInput input)
    {
        var parts = new List<string>();

        if (input.EmployeeStatus.HasValue)
        {
            parts.Add("Employee Status : " + input.EmployeeStatus.Value);
        }

        if (!input.NationalityCode.IsNullOrWhiteSpace())
        {
            parts.Add("Nationality : " + input.NationalityCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptSalaryByNationality.ascx.cs:499-598).
    /// </summary>
    private async Task<IQueryable<SalaryByNationalityReportRowDto>> BuildQueryAsync(
        GetSalaryByNationalityReportInput input)
    {
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;
        var nationalityCode = input.NationalityCode;
        var today = Today;

        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        // Legacy lines 526-529: basic-pay rows current TODAY, employee must have a nationality.
        var baseQuery = _employeeBasicPayRepository.GetAll()
            .Where(p => p.StartDate.Date <= today
                        && p.EndDate.Date >= today
                        && p.Employee.NationalityId != 0);

        // Legacy line 531 -- evaluated on the UNFILTERED base query on purpose (see class remarks).
        var totalAmount = await baseQuery.SumAsync(p => (double?)p.Amount) ?? 0d;

        var filtered = baseQuery
            // Legacy lines 537-538.
            .WhereIf(employeeStatus.HasValue, p => p.Employee.Status == employeeStatus.Value)
            // Legacy lines 544-545: the report-specific cmbNationality, matched on the CODE.
            .WhereIf(!nationalityCode.IsNullOrWhiteSpace(),
                p => p.Employee.Nationality.Code == nationalityCode)
            // Legacy line 548 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, p => rlsEmployeeIds.Contains(p.EmployeeId));

        // Legacy lines 550-568: group by nationality, sum the amounts, count the rows, then derive the
        // percentage against the pre-filter total.
        return filtered
            .GroupBy(p => new
            {
                p.Employee.NationalityId,
                p.Employee.Nationality.Code,
                p.Employee.Nationality.NameEn,
                p.Employee.Nationality.NameAr
            })
            .Select(g => new SalaryByNationalityReportRowDto
            {
                Id = g.Key.NationalityId,
                NationalityCode = g.Key.Code,
                NationalityName = useArabicNames ? g.Key.NameAr : g.Key.NameEn,
                Amount = g.Sum(p => p.Amount),
                Staff = g.Count(),
                // Legacy line 566 rounds to 3 decimals.
                Percentage = totalAmount == 0
                    ? 0
                    : System.Math.Round(g.Sum(p => p.Amount) / totalAmount * 100, 3)
            });
    }

    /// <summary>Exported columns in legacy column order. OID is omitted -- a surrogate key.</summary>
    private static IReadOnlyList<GridReportColumn<SalaryByNationalityReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<SalaryByNationalityReportRowDto>>
        {
            new("Code", r => r.NationalityCode),
            new("Nationality", r => r.NationalityName),
            new("Amount", r => r.Amount),
            new("Staff", r => r.Staff),
            new("Percentage", r => r.Percentage)
        };
    }
}
