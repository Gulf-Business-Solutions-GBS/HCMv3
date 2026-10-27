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
using HCMv3.GridReports.TrainingReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TrainingManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TrainingReport;

/// <summary>
/// Employee Training analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TrainingReport/uscRptEmployeeTraining.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 528-682).
///
/// NOT PORTED (DevExpress/WebForms UI plumbing): drill-up/drill-down session stack, ColumnChooser JSON
/// persistence, the callback-panel state machine, PDF export, and the dead <c>Table1_Old()</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeTraining)]
public class EmployeeTrainingReportAppService : GridReportAppServiceBase, IEmployeeTrainingReportAppService
{
    private readonly IRepository<EmployeeTraining> _employeeTrainingRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeTrainingReportAppService(
        IRepository<EmployeeTraining> employeeTrainingRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeTrainingRepository = employeeTrainingRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeTraining";

    protected override string ScreenCaption => "Employee Training";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTrainingReportRowDto>> GetEmployeeTrainingReport(
        GetEmployeeTrainingReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildTrainingParameterString(input), totalCount);

        return new PagedResultDto<EmployeeTrainingReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeTrainingReportToExcel(GetEmployeeTrainingReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeTraining.xlsx",
            rows,
            GetExportColumns(),
            BuildTrainingParameterString(input));
    }

    /// <summary>Legacy <c>GetParameterStr()</c> (lines 507-524) appends the training filter.</summary>
    private static string BuildTrainingParameterString(GetEmployeeTrainingReportInput input, string dummy = null)
    {
        return input.TrainingCourseCode.IsNullOrWhiteSpace()
            ? null
            : "Training : " + input.TrainingCourseCode;
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>. FromDate/ToDate are intentionally NOT applied as filters -- see the
    /// DTO remarks for the reproduced legacy quirk.
    /// </summary>
    private async Task<IQueryable<EmployeeTrainingReportRowDto>> BuildQueryAsync(
        GetEmployeeTrainingReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var trainingCourseCode = input.TrainingCourseCode;
        var today = Today;

        var query = _employeeTrainingRepository.GetAll()
            .Where(t => t.Employee != null)
            // Legacy lines 593-597.
            .WhereIf(employeeStatus.HasValue, t => t.Employee.Status == employeeStatus.Value)
            // Legacy lines 600-604.
            .WhereIf(!trainingCourseCode.IsNullOrWhiteSpace(), t => t.TrainingCourse.Code == trainingCourseCode)
            // Legacy lines 606-611.
            .WhereIf(employeeCodes.Count > 0, t => employeeCodes.Contains(t.Employee.Code))
            // Legacy line 613 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, t => rlsEmployeeIds.Contains(t.EmployeeId));

        // Legacy's dsOrgAss sub-query (lines 615-630): the assignment current AS OF TODAY.
        var currentAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        return query.Select(t => new EmployeeTrainingReportRowDto
        {
            Id = t.Id,
            EmployeeCode = t.Employee.Code,
            EmployeeName = useArabicNames ? t.Employee.FullNameAr : t.Employee.FullNameEn,
            JobTitleName = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? null : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            WorkLocationName = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? null : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            TrainingCourseName = t.TrainingCourse == null
                ? string.Empty
                : (useArabicNames ? t.TrainingCourse.NameAr : t.TrainingCourse.NameEn),
            CompletionStatus = t.CompletionStatus,
            Score = t.Score
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeTrainingReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeTrainingReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("WorkLocation", r => r.WorkLocationName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("TrainingCourse", r => r.TrainingCourseName),
            new("CompletionStatus", r => r.CompletionStatus),
            new("Score", r => r.Score)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeTrainingReportRowDto> rows)
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
