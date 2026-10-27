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
/// Late/Early Log (Custom 033) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom033.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 619-1192).
///
/// The row-shaping algorithm lives in <see cref="LateEarlyLogGeoVariantReportAppServiceBase"/>, which this
/// report shares with its near-twin <c>uscRptLateEarlyLog_Custom036</c>; read that class's remarks for the
/// legacy defects reproduced (hard-coded night shift, ignored time-management status, transaction-type-
/// blind punch selection) and the two unreachable legacy branches flagged there. This class contributes
/// only the filter panel, the column set and the permission.
///
/// COLUMN SET: Code, Employee, AccessId, OrgLevel1..OrgLevel7, Date, WeekDay, Shift Start, Shift End,
/// Sign In, Sign Out, Total Hours, Status, PunchInAccessUnit, PunchOutAccessUnit, WorkLocation, Country.
/// The two access-unit columns are exactly what Custom036 drops.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 1194-2098).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom033)]
public class LateEarlyLogCustom033ReportAppService : LateEarlyLogGeoVariantReportAppServiceBase,
    ILateEarlyLogCustom033ReportAppService
{
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public LateEarlyLogCustom033ReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        LateEarlyLogCustomDataLoader dataLoader,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
        : base(employeeRepository, orgAssignmentRepository, dataLoader)
    {
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptLateEarlyLog_Custom033";

    protected override string ScreenCaption => "Late Early Log (Custom 033)";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogCustom033ReportRowDto>> GetLateEarlyLogCustom033Report(
        GetLateEarlyLogCustom033ReportInput input)
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
            BuildCustom033ParameterString(input),
            totalCount);

        return new PagedResultDto<LateEarlyLogCustom033ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogCustom033ReportToExcel(GetLateEarlyLogCustom033ReportInput input)
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
            "LateEarlyLogCustom033.xlsx",
            rows,
            GetExportColumns(),
            BuildCustom033ParameterString(input));
    }

    private string BuildCustom033ParameterString(GetLateEarlyLogCustom033ReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// </summary>
    private async Task<IQueryable<LateEarlyLogCustom033ReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogCustom033ReportInput input)
    {
        var rows = await BuildRowsAsync(input, input.PayrollAreaCode);

        return rows
            .Select(r => new LateEarlyLogCustom033ReportRowDto
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
                PunchInAccessUnit = r.PunchInAccessUnit,
                PunchOutAccessUnit = r.PunchOutAccessUnit,
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
    private async Task StampOrgLevelsAsync(List<LateEarlyLogCustom033ReportRowDto> rows)
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
    /// The exported column set, in legacy column order (lines 694-726). The technical Oid row-sequence
    /// column is omitted because it carries no information the reader does not already have. Note that
    /// legacy places PunchInAccessUnit / PunchOutAccessUnit AFTER Status, not next to the punches.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogCustom033ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogCustom033ReportRowDto>>
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
            new("PunchInAccessUnit", r => r.PunchInAccessUnit),
            new("PunchOutAccessUnit", r => r.PunchOutAccessUnit),
            new("WorkLocation", r => r.WorkLocationName),
            new("Country", r => r.CountryName)
        };
    }
}
