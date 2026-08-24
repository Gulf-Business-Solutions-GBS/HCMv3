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
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Late/Early Log (Custom 036) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom036.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 617-1116).
///
/// This report IS <c>uscRptLateEarlyLog_Custom033</c> with two columns removed. A line-by-line diff of the
/// two live <c>Table1()</c> bodies shows their only behavioural difference is that Custom036 drops
/// <c>PunchInAccessUnit</c> and <c>PunchOutAccessUnit</c>. The row-shaping algorithm therefore lives once
/// in <see cref="LateEarlyLogGeoVariantReportAppServiceBase"/> -- read that class's remarks for the legacy
/// defects reproduced (hard-coded night shift, ignored time-management status, transaction-type-blind
/// punch selection) and the two unreachable legacy branches flagged there. This class contributes only the
/// filter panel, the column set and the permission.
///
/// COLUMN SET: Code, Employee, AccessId, OrgLevel1..OrgLevel7, Date, WeekDay, Shift Start, Shift End,
/// Sign In, Sign Out, Total Hours, Status, WorkLocation, Country.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 1118-2012).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom036)]
public class LateEarlyLogCustom036ReportAppService : LateEarlyLogGeoVariantReportAppServiceBase,
    ILateEarlyLogCustom036ReportAppService
{
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public LateEarlyLogCustom036ReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        LateEarlyLogCustomDataLoader dataLoader,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
        : base(employeeRepository, orgAssignmentRepository, dataLoader)
    {
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptLateEarlyLog_Custom036";

    protected override string ScreenCaption => "Late Early Log (Custom 036)";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogCustom036ReportRowDto>> GetLateEarlyLogCustom036Report(
        GetLateEarlyLogCustom036ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildCustom036ParameterString(input),
            totalCount);

        return new PagedResultDto<LateEarlyLogCustom036ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogCustom036ReportToExcel(GetLateEarlyLogCustom036ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "LateEarlyLogCustom036.xlsx",
            rows,
            GetExportColumns(),
            BuildCustom036ParameterString(input));
    }

    private string BuildCustom036ParameterString(GetLateEarlyLogCustom036ReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// The two access-unit values the shared row carries are deliberately dropped here -- that is the
    /// whole of Custom036's difference from Custom033.
    /// </summary>
    private async Task<IQueryable<LateEarlyLogCustom036ReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogCustom036ReportInput input)
    {
        var rows = await BuildRowsAsync(input, input.PayrollAreaCode);

        return rows
            .Select(r => new LateEarlyLogCustom036ReportRowDto
            {
                Id = r.Id,
                EmployeeCode = r.EmployeeCode,
                EmployeeName = r.EmployeeName,
                AccessId = r.AccessId,
                OrganizationUnitId = r.OrganizationUnitId,
                Date = r.Date,
                WeekDay = r.WeekDay,
                ShiftStart = r.ShiftStart,
                ShiftEnd = r.ShiftEnd,
                SignIn = r.SignIn,
                SignOut = r.SignOut,
                TotalHours = r.TotalHours,
                Status = r.Status,
                WorkLocationName = r.WorkLocationName,
                CountryName = r.CountryName
            })
            .ToList()
            .AsQueryable();
    }

    /// <summary>
    /// Fills the seven legacy OrgLevel columns after materialization. Shared by the read and the export so
    /// the two can never drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<LateEarlyLogCustom036ReportRowDto> rows)
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

    /// <summary>
    /// The exported column set, in legacy column order (lines 688-718). The technical Oid row-sequence
    /// column is omitted because it carries no information the reader does not already have.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogCustom036ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogCustom036ReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("AccessId", r => r.AccessId),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Date", r => r.Date),
            new("WeekDay", r => r.WeekDay),
            new("ShiftStart", r => r.ShiftStart),
            new("ShiftEnd", r => r.ShiftEnd),
            new("SignIn", r => r.SignIn),
            new("SignOut", r => r.SignOut),
            new("TotalHours", r => r.TotalHours),
            new("Status", r => r.Status),
            new("WorkLocation", r => r.WorkLocationName),
            new("Country", r => r.CountryName)
        };
    }
}
