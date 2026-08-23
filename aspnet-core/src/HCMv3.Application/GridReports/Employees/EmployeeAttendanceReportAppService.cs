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
/// Employee Attendance analysis grid report -- port of legacy
/// <c>uscRptEmployeeAttendance.ascx(.cs)</c>, method <c>Table1()</c> (lines 473-594).
///
/// NOT PORTED (DevExpress/WebForms UI plumbing): the drill-up/drill-down session stack (single level --
/// <c>GetTable()</c> defines only "Table1"), the ColumnChooser JSON persistence, the callback-panel
/// state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeAttendance)]
public class EmployeeAttendanceReportAppService : GridReportAppServiceBase, IEmployeeAttendanceReportAppService
{
    private readonly IRepository<EmployeeAttendance> _employeeAttendanceRepository;

    public EmployeeAttendanceReportAppService(IRepository<EmployeeAttendance> employeeAttendanceRepository)
    {
        _employeeAttendanceRepository = employeeAttendanceRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeAttendance";

    protected override string ScreenCaption => "Employee Attendance";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAttendanceReportRowDto>> GetEmployeeAttendanceReport(
        GetEmployeeAttendanceReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeAttendanceReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeAttendanceReportToExcel(GetEmployeeAttendanceReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeAttendance.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeAttendance.ascx.cs:473-594).
    /// </summary>
    private async Task<IQueryable<EmployeeAttendanceReportRowDto>> BuildQueryAsync(
        GetEmployeeAttendanceReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        var query = _employeeAttendanceRepository.GetAll()
            // Legacy lines 477-481 spell the overlap out as four alternatives:
            //   (Start <= From <= End) || (Start <= To <= End) || (From <= Start <= To) || (From <= End <= To)
            // which is exactly StartDate <= To && EndDate >= From for any record with Start <= End.
            .Where(a => a.StartDate <= toDate && a.EndDate >= fromDate)
            // Legacy lines 486-490: Employee.Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, a => a.Employee.Status == employeeStatus.Value)
            // Legacy lines 491-495: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, a => employeeCodes.Contains(a.Employee.Code))
            // Legacy line 497 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId));

        return query.Select(a => new EmployeeAttendanceReportRowDto
        {
            Id = a.Id,
            EmployeeCode = a.Employee.Code,
            EmployeeName = useArabicNames ? a.Employee.FullNameAr : a.Employee.FullNameEn,
            StartDate = a.StartDate,
            // Legacy line 544 filled this cell from rec.StartDate -- a defect, see the DTO remarks.
            EndDate = a.EndDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            AttendanceTypeName = a.AttendanceType == null
                ? string.Empty
                : (useArabicNames ? a.AttendanceType.NameAr : a.AttendanceType.NameEn),
            AttendanceHours = a.AttendanceHours,
            TotalAttendDays = a.TotalAttendDays,
            Comments = a.Comments
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeAttendanceReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeAttendanceReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("StartTime", r => r.StartTime),
            new("EndTime", r => r.EndTime),
            new("AttendanceType", r => r.AttendanceTypeName),
            new("AttendanceHours", r => r.AttendanceHours),
            new("TotalAttendDays", r => r.TotalAttendDays),
            new("Comments", r => r.Comments)
        };
    }
}
