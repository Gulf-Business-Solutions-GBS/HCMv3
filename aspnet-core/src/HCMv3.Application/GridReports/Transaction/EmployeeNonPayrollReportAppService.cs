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
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Non Payroll Transaction analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeNonPayroll.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 502-660).
///
/// NOT PORTED: the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export
/// button (legacy had no drill-down on this screen).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeNonPayroll)]
public class EmployeeNonPayrollReportAppService : GridReportAppServiceBase, IEmployeeNonPayrollReportAppService
{
    private readonly IRepository<EmployeeNonPayrollTrans> _employeeNonPayrollTransRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeNonPayrollReportAppService(
        IRepository<EmployeeNonPayrollTrans> employeeNonPayrollTransRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeNonPayrollTransRepository = employeeNonPayrollTransRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeNonPayroll";

    protected override string ScreenCaption => "Employee Non Payroll Transaction";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeNonPayrollReportRowDto>> GetEmployeeNonPayrollReport(
        GetEmployeeNonPayrollReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeNonPayrollReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeNonPayrollReportToExcel(GetEmployeeNonPayrollReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var rows = await query.OrderBy(input.Sorting).ToListAsync();
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync("EmployeeNonPayroll.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<IQueryable<EmployeeNonPayrollReportRowDto>> BuildQueryAsync(GetEmployeeNonPayrollReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var wageTypeCode = input.WageTypeCode;
        var today = Today;

        var query = _employeeNonPayrollTransRepository.GetAll()
            .Where(t => t.PaymentDate >= fromDate && t.PaymentDate <= toDate)
            .WhereIf(!wageTypeCode.IsNullOrWhiteSpace(), t => t.WageType.Code == wageTypeCode)
            .WhereIf(employeeCodes.Count > 0, t => employeeCodes.Contains(t.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, t => rlsEmployeeIds.Contains(t.EmployeeId));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        return query.Select(t => new EmployeeNonPayrollReportRowDto
        {
            Id = t.Id,
            EmployeeCode = t.Employee.Code,
            EmployeeName = useArabicNames ? t.Employee.FullNameAr : t.Employee.FullNameEn,
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == t.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            WageTypeName = t.WageType == null ? string.Empty : (useArabicNames ? t.WageType.NameAr : t.WageType.NameEn),
            PaymentDate = t.PaymentDate,
            Amount = t.Amount,
            CurrencyShortForm = t.Currency == null
                ? string.Empty
                : (useArabicNames ? t.Currency.CurrencyShortFormAr : t.Currency.CurrencyShortFormEn),
            IsLocked = t.IsLocked,
            PayableBy = t.PayableBy,
            PaymentMethod = t.PaymentMethod
        });
    }

    private async Task StampOrgLevelsAsync(List<EmployeeNonPayrollReportRowDto> rows)
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

    private string BuildParameterString(GetEmployeeNonPayrollReportInput input)
    {
        var parts = new List<string>
        {
            "From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"),
            "To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy")
        };

        if (!input.WageTypeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Wage Type : " + input.WageTypeCode);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<EmployeeNonPayrollReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeNonPayrollReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WageType", r => r.WageTypeName),
            new("PaymentDate", r => r.PaymentDate),
            new("Amount", r => r.Amount),
            new("Currency", r => r.CurrencyShortForm),
            new("IsLocked", r => r.IsLocked),
            new("PayableBy", r => r.PayableBy),
            new("PaymentMethod", r => r.PaymentMethod)
        };
    }
}
