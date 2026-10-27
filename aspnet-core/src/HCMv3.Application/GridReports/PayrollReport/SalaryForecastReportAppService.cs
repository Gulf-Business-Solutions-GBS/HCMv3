using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Salary Forecast grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryForecast.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 479-603).
///
/// NOT PORTED: <c>Table2()</c> employee-level drill-down (see <see cref="SalaryForecastReportRowDto"/>
/// remarks), ColumnChooser JSON, callback-panel state machine.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SalaryForecast)]
public class SalaryForecastReportAppService : GridReportAppServiceBase, ISalaryForecastReportAppService
{
    private const int YearCount = 5;
    private const string BasicPayWageTypeCode = "1000";

    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public SalaryForecastReportAppService(
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _basicPayDetailRepository = basicPayDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptSalaryForecast";

    protected override string ScreenCaption => "Salary Forecast";

    [HttpPost]
    public async Task<SalaryForecastReportOutput> GetSalaryForecastReport(GetSalaryForecastReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();
        var paged = ordered.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new SalaryForecastReportOutput
        {
            TotalCount = totalCount,
            Items = paged,
            ForecastYears = ForecastYears()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetSalaryForecastReportToExcel(GetSalaryForecastReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();
        var years = ForecastYears();

        var columns = new List<GridReportColumn<SalaryForecastReportRowDto>>
        {
            new("Company", r => r.CompanyName),
            new("Department", r => r.DepartmentName),
            GridReportColumn<SalaryForecastReportRowDto>.FromLiteralHeader(
                "Current Year (" + Today.Year + ")", r => r.CurrentAmount)
        };

        for (var i = 0; i < YearCount; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<SalaryForecastReportRowDto>.FromLiteralHeader(
                "Forecast (" + years[index] + ")", r => r.ForecastAmounts[index]));
        }

        return await ExportToFileAsync("SalaryForecast.xlsx", ordered, columns, BuildParameterString(input));
    }

    private async Task<List<SalaryForecastReportRowDto>> BuildRowsAsync(GetSalaryForecastReportInput input)
    {
        var salaryChangePercent = input.SalaryChangePercent ?? 0;
        var useArabicNames = UseArabicNames;

        // Legacy lines 545-547: EmployeeBasicPayDetail active AS OF NOW, regardless of the picked date --
        // see class remarks.
        var wageQuery = _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= Today && d.EmployeeBasicPay.EndDate >= Today);

        if (input.ForecastType == SalaryForecastType.BasicPay)
        {
            wageQuery = wageQuery.Where(d => d.WageType.Code == BasicPayWageTypeCode);
        }

        var basicPay = await wageQuery
            .Select(d => new { EmployeeId = d.EmployeeBasicPay.EmployeeId, d.Amount })
            .ToListAsync();

        if (basicPay.Count == 0)
        {
            return new List<SalaryForecastReportRowDto>();
        }

        var employeeIds = basicPay.Select(b => b.EmployeeId).Distinct().ToList();

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= Today && o.EndDate >= Today)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId })
            .ToListAsync();

        var orgAssignmentByEmployee = orgAssignments
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First().OrganizationUnitId);

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var grouped = basicPay
            .Select(b =>
            {
                var levels = orgAssignmentByEmployee.TryGetValue(b.EmployeeId, out var unitId)
                    ? lookup.GetLevelNames(unitId)
                    : new[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
                return new { Company = levels[0], Department = levels[1], b.Amount };
            })
            .GroupBy(b => new { b.Company, b.Department })
            .Select(g => new { g.Key.Company, g.Key.Department, Total = g.Sum(b => b.Amount) });

        var rows = new List<SalaryForecastReportRowDto>();

        foreach (var g in grouped)
        {
            var forecast = new double[YearCount];
            var lastValue = g.Total;
            for (var i = 0; i < YearCount; i++)
            {
                lastValue += lastValue * salaryChangePercent / 100;
                forecast[i] = lastValue;
            }

            rows.Add(new SalaryForecastReportRowDto
            {
                CompanyName = g.Company,
                DepartmentName = g.Department,
                CurrentAmount = g.Total,
                ForecastAmounts = forecast
            });
        }

        return rows;
    }

    private List<int> ForecastYears()
    {
        var currentYear = Today.Year;
        return Enumerable.Range(1, YearCount).Select(i => currentYear + i).ToList();
    }

    private string BuildParameterString(GetSalaryForecastReportInput input)
    {
        var parts = new List<string>
        {
            "Effective Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy")
        };

        if (input.ForecastType.HasValue)
        {
            parts.Add("Forecast Type : " + input.ForecastType.Value);
        }

        if (input.SalaryChangePercent.HasValue)
        {
            parts.Add("Salary Change (%) : " + input.SalaryChangePercent.Value);
        }

        return string.Join(" | ", parts);
    }
}
