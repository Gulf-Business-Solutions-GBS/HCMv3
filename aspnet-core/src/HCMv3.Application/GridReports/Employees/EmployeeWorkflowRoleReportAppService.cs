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
using HCMv3.SystemSecurity;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Workflow Role analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeWorkflowRole.ascx(.cs)</c>.
///
/// Ports <c>Table1()</c> (lines 494-570) exactly: the same overlap predicate, the same three filters, the
/// same six columns.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (only ever one level, <c>GetKey()</c>/<c>GetTable()</c> define a single "Table1"); the
/// ColumnChooser JSON persistence; the callback-panel state machine; the PDF export button; and the dead
/// <c>Table1_Old()</c> (lines 576+), an earlier formulation of the same query.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeWorkflowRole)]
public class EmployeeWorkflowRoleReportAppService : GridReportAppServiceBase,
    IEmployeeWorkflowRoleReportAppService
{
    private readonly IRepository<EmployeeWorkflowRole> _employeeWorkflowRoleRepository;

    public EmployeeWorkflowRoleReportAppService(IRepository<EmployeeWorkflowRole> employeeWorkflowRoleRepository)
    {
        _employeeWorkflowRoleRepository = employeeWorkflowRoleRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeWorkflowRole";

    protected override string ScreenCaption => "Employee Workflow Role";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeWorkflowRoleReportRowDto>> GetEmployeeWorkflowRoleReport(
        GetEmployeeWorkflowRoleReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeWorkflowRoleReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeWorkflowRoleReportToExcel(GetEmployeeWorkflowRoleReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeWorkflowRole.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeWorkflowRole.ascx.cs:494-570).
    /// </summary>
    private async Task<IQueryable<EmployeeWorkflowRoleReportRowDto>> BuildQueryAsync(
        GetEmployeeWorkflowRoleReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;

        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        var query = _employeeWorkflowRoleRepository.GetAll()
            // Legacy lines 522-523: !(r.StartDate.Date > dtTo.Date || r.EndDate.Date < dtFrom.Date).
            .Where(r => r.StartDate.Date <= toDate && r.EndDate.Date >= fromDate)
            // Legacy line 531 (cmbRequestStatus is bound to EmployeeStatus, see the input DTO remarks).
            .WhereIf(employeeStatus.HasValue, r => r.Employee.Status == employeeStatus.Value)
            // Legacy lines 535-536: employees.Contains(r.Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // Legacy line 539 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId));

        return query.Select(r => new EmployeeWorkflowRoleReportRowDto
        {
            // Legacy "OID" column. Exposed as Id because it is the row key, not a data column.
            Id = r.Id,
            EmployeeCode = r.Employee.Code,
            EmployeeName = useArabicNames ? r.Employee.FullNameAr : r.Employee.FullNameEn,
            // Legacy line 545: empty string, not null, when the row has no workflow role.
            WorkflowRoleName = r.WorkflowRole == null
                ? string.Empty
                : (useArabicNames ? r.WorkflowRole.NameAr : r.WorkflowRole.NameEn),
            StartDate = r.StartDate,
            EndDate = r.EndDate
        });
    }

    /// <summary>Exported columns, in legacy column order (OID omitted -- a surrogate key).</summary>
    private static IReadOnlyList<GridReportColumn<EmployeeWorkflowRoleReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeWorkflowRoleReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("WorkflowRole", r => r.WorkflowRoleName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate)
        };
    }
}
