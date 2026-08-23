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
/// Employee Action analysis grid report -- port of legacy <c>uscRptEmployeeAction.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 484-643).
///
/// ATYPICAL: this report has NO employee-status filter (its legacy panel has no <c>cmbEmpStatus</c>);
/// its report-specific filter is <c>cmbPersonalAction</c> instead. Do not copy the missing status
/// filter into other reports.
///
/// NOT PORTED (DevExpress/WebForms UI plumbing): the drill-up/drill-down session stack (single level --
/// <c>GetKey()</c>/<c>GetTable()</c> define only "Table1"), the ColumnChooser JSON persistence, the
/// callback-panel state machine, the PDF export button and the dead <c>Table1_Old()</c> (lines 645+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeAction)]
public class EmployeeActionReportAppService : GridReportAppServiceBase, IEmployeeActionReportAppService
{
    private readonly IRepository<EmployeeAction> _employeeActionRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public EmployeeActionReportAppService(
        IRepository<EmployeeAction> employeeActionRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _employeeActionRepository = employeeActionRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeAction";

    protected override string ScreenCaption => "Employee Action";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeActionReportRowDto>> GetEmployeeActionReport(
        GetEmployeeActionReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildActionParameterString(input), totalCount);

        return new PagedResultDto<EmployeeActionReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeActionReportToExcel(GetEmployeeActionReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeAction.xlsx",
            rows,
            GetExportColumns(),
            BuildActionParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (uscRptEmployeeAction.ascx.cs:463-477): personal action, employee
    /// code and the two dates. The base helper also emits an "Employee Status" part when one is set;
    /// this report never sets one, so the two strings agree apart from the extra part's position.
    /// </summary>
    private string BuildActionParameterString(GetEmployeeActionReportInput input)
    {
        return BuildParameterString(
            input,
            input.PersonalActionCode.IsNullOrWhiteSpace() ? null : "Personal Action : " + input.PersonalActionCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeAction.ascx.cs:484-643).
    /// </summary>
    private async Task<IQueryable<EmployeeActionReportRowDto>> BuildQueryAsync(GetEmployeeActionReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var personalActionCode = input.PersonalActionCode;
        var hasPersonalActionFilter = !personalActionCode.IsNullOrWhiteSpace();

        // Legacy line 551: the org-assignment lookup is keyed on DateTime.Now, NOT the report dates.
        var today = Today;

        var query = _employeeActionRepository.GetAll()
            // Legacy lines 500-501: !(StartDate > dtTo || EndDate < dtFrom). The legacy EndDate property
            // is a getter that returns StartDate (HCMv3 therefore stores no EndDate), so the overlap
            // test collapses to "StartDate falls inside the window".
            .Where(a => a.StartDate.Date <= toDate && a.StartDate.Date >= fromDate)
            // Legacy lines 505-508: PersonalAction.Code == cmbPersonalAction, only when one was picked.
            .WhereIf(hasPersonalActionFilter, a => a.PersonalAction.Code == personalActionCode)
            // Legacy lines 509-513: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, a => employeeCodes.Contains(a.Employee.Code))
            // Legacy line 515 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId));

        return query.Select(a => new EmployeeActionReportRowDto
        {
            Id = a.Id,
            EmployeeCode = a.Employee.Code,
            EmployeeName = useArabicNames ? a.Employee.FullNameAr : a.Employee.FullNameEn,
            // Legacy lines 551-570 + 617-627: JobTitle / OrgLevel1..7 / WorkLocation all come from the
            // SAME org assignment -- the one current TODAY, not as of the report dates.
            JobTitleName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            // Legacy line 587: the EMPLOYEE's country (not the payroll area's).
            CountryName = a.Employee.Country == null
                ? string.Empty
                : (useArabicNames ? a.Employee.Country.NameAr : a.Employee.Country.NameEn),
            // Legacy OrgLevel1..7 collapsed to one OrganizationUnit column -- see the DTO remarks.
            OrganizationUnitName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnit == null ? null : o.OrganizationUnit.DisplayName)
                .FirstOrDefault(),
            WorkLocationName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            StartDate = a.StartDate,
            // Legacy EndDate == StartDate by definition -- see the DTO remarks.
            EndDate = a.StartDate,
            PersonalActionName = a.PersonalAction == null
                ? string.Empty
                : (useArabicNames ? a.PersonalAction.NameAr : a.PersonalAction.NameEn),
            ActionReasonName = a.ActionReason == null
                ? string.Empty
                : (useArabicNames ? a.ActionReason.NameAr : a.ActionReason.NameEn)
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted and the seven
    /// OrgLevel columns are the single OrganizationUnit column.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeActionReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeActionReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("Country", r => r.CountryName),
            new("OrganizationUnit", r => r.OrganizationUnitName),
            new("WorkLocation", r => r.WorkLocationName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("PersonalAction", r => r.PersonalActionName),
            new("ActionReason", r => r.ActionReasonName)
        };
    }
}
