using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
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
/// Employee Bonus analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeBonus.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 449-563).
///
/// ATYPICAL vs the reference report: this is a PIVOT. Legacy built one column per calendar year between
/// the min and max PaymentDate.Year of EVERY wage-type "1053" (Bonus Pay) transaction in the database --
/// deliberately NOT restricted by the employee filters, the RLS list, or any date range (lines 455-459) --
/// then filled each employee's row with that year's bonus total.
///
/// The row set itself is Employees (not transactions), so an employee with no bonus at all still appears
/// with zeroes; that is legacy behaviour (lines 495-551 iterate the employee list, not the bonus list).
///
/// NOT PORTED (DevExpress/WebForms plumbing with no data meaning): the drill-up/drill-down session stack
/// (single-level here -- GetKey()/GetTable() define only "Table1", lines 369-405), the ColumnChooser JSON
/// persistence, the callback-panel state machine, the PDF export button, and the dead Table1_Old()
/// (lines 568+), an earlier formulation of the same pivot.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeBonus)]
public class EmployeeBonusReportAppService : GridReportAppServiceBase, IEmployeeBonusReportAppService
{
    /// <summary>Legacy line 452: "1053 : Bonus Pay".</summary>
    private const string BonusWageTypeCode = "1053";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeNonPayrollTrans> _nonPayrollTransRepository;

    public EmployeeBonusReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeNonPayrollTrans> nonPayrollTransRepository)
    {
        _employeeRepository = employeeRepository;
        _nonPayrollTransRepository = nonPayrollTransRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeBonus";

    protected override string ScreenCaption => "Employee Bonus";

    [HttpPost]
    public async Task<EmployeeBonusReportOutput> GetEmployeeBonusReport(GetEmployeeBonusReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var years = await FillBonusColumnsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new EmployeeBonusReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            Years = years
        };
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeBonusReportToExcel(GetEmployeeBonusReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        var years = await FillBonusColumnsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeBonus.xlsx",
            rows,
            GetExportColumns(years),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND the row projection, shared by the paged read and the
    /// export -- this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports the "Get Data" region of legacy Table1() (lines 495-524).
    ///
    /// The pivot values are NOT part of this query; see <see cref="FillBonusColumnsAsync"/>.
    /// </summary>
    private async Task<IQueryable<EmployeeBonusReportRowDto>> BuildQueryAsync(GetEmployeeBonusReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var employeeStatus = input.EmployeeStatus;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        // NOTE: legacy applied NO date filter here -- both date pickers are Visible="false" on this screen.
        var query = _employeeRepository.GetAll()
            // Legacy lines 501-505.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            // Legacy lines 506-510: employees.Contains(r.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy line 512 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        return query.Select(e => new EmployeeBonusReportRowDto
        {
            // Legacy "OID" column (line 530).
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn
        });
    }

    /// <summary>
    /// Ports the pivot half of legacy Table1(): the year column set (lines 455-495) and the per-employee
    /// yearly totals plus the two averages (lines 526-556).
    ///
    /// Runs AFTER materialization because the number of columns is data-dependent, and it is shared by the
    /// paged read and the export so the two cannot drift. Returns the generated year list.
    /// </summary>
    private async Task<List<int>> FillBonusColumnsAsync(List<EmployeeBonusReportRowDto> rows)
    {
        // Legacy lines 452-459: the year SPAN comes from every 1053 transaction in the database, with no
        // employee/RLS/date filtering whatsoever.
        var bonusQuery = _nonPayrollTransRepository.GetAll()
            .Where(t => t.WageType != null && t.WageType.Code == BonusWageTypeCode);

        var minYear = await bonusQuery.Select(t => (int?)t.PaymentDate.Year).MinAsync() ?? 0;
        var maxYear = await bonusQuery.Select(t => (int?)t.PaymentDate.Year).MaxAsync() ?? 0;

        var years = new List<int>();
        if (minYear != 0)
        {
            for (var year = minYear; year <= maxYear; year++)
            {
                years.Add(year);
            }
        }

        if (rows.Count == 0)
        {
            return years;
        }

        var employeeIds = rows.Select(r => r.Id).ToList();

        var totals = await bonusQuery
            .Where(t => employeeIds.Contains(t.EmployeeId))
            .GroupBy(t => new { t.EmployeeId, t.PaymentDate.Year })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.Year,
                Amount = g.Sum(t => t.Amount)
            })
            .ToListAsync();

        var totalsByEmployee = totals
            .GroupBy(t => t.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Year, t => t.Amount));

        foreach (var row in rows)
        {
            totalsByEmployee.TryGetValue(row.Id, out var byYear);

            double totalBonus = 0;
            double nonZeroYearCount = 0;

            row.YearlyAmounts = new List<EmployeeBonusYearAmountDto>(years.Count);

            foreach (var year in years)
            {
                double amount = 0;
                if (byYear != null && byYear.TryGetValue(year, out var yearAmount))
                {
                    amount = yearAmount;
                }

                row.YearlyAmounts.Add(new EmployeeBonusYearAmountDto { Year = year, Amount = amount });

                totalBonus += amount;

                // Legacy line 546: only years with a NON-ZERO bonus count towards the average.
                if (amount != 0)
                {
                    nonZeroYearCount++;
                }
            }

            row.AverageBonusAmount = nonZeroYearCount != 0 ? totalBonus / nonZeroYearCount : 0;
            row.MonthlyAverageBonusAmount = row.AverageBonusAmount / 12;
        }

        return years;
    }

    /// <summary>
    /// Exported columns in legacy column order (Code, Name, one per year, Average, MonthlyAverage).
    /// The technical OID key is omitted, as in the reference port.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeBonusReportRowDto>> GetExportColumns(List<int> years)
    {
        var columns = new List<GridReportColumn<EmployeeBonusReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName)
        };

        for (var i = 0; i < years.Count; i++)
        {
            var index = i;

            // The year itself is the caption -- a number, not a translatable label, so it must go in as a
            // LITERAL header (a localization key would come back bracketed as "[2020]").
            columns.Add(GridReportColumn<EmployeeBonusReportRowDto>.FromLiteralHeader(
                years[i].ToString(),
                r => index < r.YearlyAmounts.Count ? r.YearlyAmounts[index].Amount : 0));
        }

        columns.Add(new GridReportColumn<EmployeeBonusReportRowDto>(
            "AverageBonusAmount", r => r.AverageBonusAmount));
        columns.Add(new GridReportColumn<EmployeeBonusReportRowDto>(
            "MonthlyAverageBonusAmount", r => r.MonthlyAverageBonusAmount));

        return columns;
    }
}
