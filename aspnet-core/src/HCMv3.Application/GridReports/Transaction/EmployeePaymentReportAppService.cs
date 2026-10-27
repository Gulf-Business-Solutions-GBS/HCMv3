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
/// Employee Payment analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeePayment.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 502-659).
///
/// DEVIATION: legacy's <c>ExpectedAmount</c> column came from <c>EmployeePayment.GetExpectedAmount()</c>,
/// a computed wage-rate lookup that was NOT ported onto the HCMv3 <c>EmployeePayment</c> entity (see the
/// entity's own remarks -- it depends on <c>EmployeeBasicPayDetail</c>/payroll-settings rate calculation).
/// This column is therefore always 0 here; a real implementation belongs in the Payroll engine, not this
/// report.
///
/// NOT PORTED: the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export
/// button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeePayment)]
public class EmployeePaymentReportAppService : GridReportAppServiceBase, IEmployeePaymentReportAppService
{
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeePaymentReportAppService(
        IRepository<EmployeePayment> employeePaymentRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeePaymentRepository = employeePaymentRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeePayment";

    protected override string ScreenCaption => "Employee Payment";

    [HttpPost]
    public async Task<PagedResultDto<EmployeePaymentReportRowDto>> GetEmployeePaymentReport(GetEmployeePaymentReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeePaymentReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeePaymentReportToExcel(GetEmployeePaymentReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var rows = await query.OrderBy(input.Sorting).ToListAsync();
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync("EmployeePayment.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<IQueryable<EmployeePaymentReportRowDto>> BuildQueryAsync(GetEmployeePaymentReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var wageTypeCode = input.WageTypeCode;
        var today = Today;

        var query = _employeePaymentRepository.GetAll()
            .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .WhereIf(!wageTypeCode.IsNullOrWhiteSpace(), p => p.WageType.Code == wageTypeCode)
            .WhereIf(employeeCodes.Count > 0, p => employeeCodes.Contains(p.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, p => rlsEmployeeIds.Contains(p.EmployeeId));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        return query.Select(p => new EmployeePaymentReportRowDto
        {
            Id = p.Id,
            EmployeeCode = p.Employee.Code,
            EmployeeName = useArabicNames ? p.Employee.FullNameAr : p.Employee.FullNameEn,
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == p.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == p.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            WageTypeName = p.WageType == null ? string.Empty : (useArabicNames ? p.WageType.NameAr : p.WageType.NameEn),
            PaymentDate = p.PaymentDate,
            UnitType = p.UnitType.ToString(),
            Number = p.Amount,
            CurrencyShortForm = p.Currency == null
                ? string.Empty
                : (useArabicNames ? p.Currency.CurrencyShortFormAr : p.Currency.CurrencyShortFormEn),
            CostCenterName = p.CostCenter == null ? string.Empty : (useArabicNames ? p.CostCenter.NameAr : p.CostCenter.NameEn),
            // Not ported on the entity -- see class remarks.
            ExpectedAmount = 0
        });
    }

    private async Task StampOrgLevelsAsync(List<EmployeePaymentReportRowDto> rows)
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

    private string BuildParameterString(GetEmployeePaymentReportInput input)
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

    private static IReadOnlyList<GridReportColumn<EmployeePaymentReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeePaymentReportRowDto>>
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
            new("UnitType", r => r.UnitType),
            new("Number", r => r.Number),
            new("Currency", r => r.CurrencyShortForm),
            new("CostCenter", r => r.CostCenterName),
            new("ExpectedAmount", r => r.ExpectedAmount)
        };
    }
}
