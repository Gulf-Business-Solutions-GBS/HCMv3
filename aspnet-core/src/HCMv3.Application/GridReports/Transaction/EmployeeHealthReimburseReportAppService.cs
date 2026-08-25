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
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Health Reimburse analysis grid report -- reduced-scope port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeHealthReimburse.ascx(.cs)</c>.
/// See <see cref="GetEmployeeHealthReimburseReportInput"/> remarks for why the eligibility/quota/accrual
/// columns (legacy's actual report focus) are out of scope here, and why this queries
/// <c>EmployeeHealthReimburse</c> transactions directly instead of legacy's Employee-driven query.
///
/// NOT PORTED (DevExpress/WebForms UI plumbing, no data meaning): the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead "Table1_Old"-equivalent code paths.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeHealthReimburse)]
public class EmployeeHealthReimburseReportAppService : GridReportAppServiceBase, IEmployeeHealthReimburseReportAppService
{
    private readonly IRepository<EmployeeHealthReimburse> _employeeHealthReimburseRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeHealthReimburseReportAppService(
        IRepository<EmployeeHealthReimburse> employeeHealthReimburseRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeHealthReimburseRepository = employeeHealthReimburseRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeHealthReimburse";

    protected override string ScreenCaption => "Employee Health Reimburse";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeHealthReimburseReportRowDto>> GetEmployeeHealthReimburseReport(
        GetEmployeeHealthReimburseReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeHealthReimburseReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeHealthReimburseReportToExcel(GetEmployeeHealthReimburseReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeHealthReimburse.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    private async Task<IQueryable<EmployeeHealthReimburseReportRowDto>> BuildQueryAsync(
        GetEmployeeHealthReimburseReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var employeeStatus = input.EmployeeStatus;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var query = _employeeHealthReimburseRepository.GetAll()
            .Where(h => h.PaymentDate >= fromDate && h.PaymentDate <= toDate)
            .WhereIf(employeeStatus.HasValue, h => h.Employee.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, h => employeeCodes.Contains(h.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, h => rlsEmployeeIds.Contains(h.EmployeeId));

        return query.Select(h => new EmployeeHealthReimburseReportRowDto
        {
            Id = h.Id,
            EmployeeCode = h.Employee.Code,
            EmployeeName = useArabicNames ? h.Employee.FullNameAr : h.Employee.FullNameEn,
            // Org assignment current as of the payment date, same "AsOnDate" semantics legacy used
            // (uscRptEmployeeHealthReimburse.ascx.cs:618, keyed off dtFromValue there).
            JobTitle = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == h.EmployeeId && o.StartDate <= h.PaymentDate && o.EndDate >= h.PaymentDate)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == h.EmployeeId && o.StartDate <= h.PaymentDate && o.EndDate >= h.PaymentDate && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            PaymentDate = h.PaymentDate,
            Amount = h.Amount,
            Comments = h.Comments
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeHealthReimburseReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeHealthReimburseReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitle),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("PaymentDate", r => r.PaymentDate),
            new("Amount", r => r.Amount),
            new("Comments", r => r.Comments)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeHealthReimburseReportRowDto> rows)
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
