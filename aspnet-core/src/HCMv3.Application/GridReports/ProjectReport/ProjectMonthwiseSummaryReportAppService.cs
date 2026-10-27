using System;
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
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.ProjectReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.ProjectReport;

/// <summary>
/// Project Monthwise Summary analysis grid report -- port of legacy
/// <c>uscRptProjectMonthwiseSummary.ascx.cs</c>, whose entire data contract is <c>Table1()</c>
/// (lines 490-701).
///
/// ATYPICAL: a per-project (WorkingDays, OTHours, Amount) pivot -- see
/// <see cref="Dto.ProjectMonthwiseSummaryReportRowDto"/> remarks for the deliberate metric-then-project to
/// project-then-metric reorganization.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, the WebChart hooks
/// (<c>GetValueDataMembers</c>/etc., dead outside the DevExpress chart control), and the dead
/// <c>Table1_Old</c> (an earlier equivalent formulation, commented out in legacy).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ProjectMonthwiseSummary)]
public class ProjectMonthwiseSummaryReportAppService : GridReportAppServiceBase, IProjectMonthwiseSummaryReportAppService
{
    private const string WorkingDaysWageTypeCode = "1000";
    private static readonly DateTime FallbackPeriod = new DateTime(1900, 1, 31);

    private readonly IRepository<ProjectRegisterDetail> _projectRegisterDetailRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public ProjectMonthwiseSummaryReportAppService(
        IRepository<ProjectRegisterDetail> projectRegisterDetailRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _projectRegisterDetailRepository = projectRegisterDetailRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptProjectMonthwiseSummary";

    protected override string ScreenCaption => "Project Monthwise Summary";

    [HttpPost]
    public async Task<PagedResultDto<ProjectMonthwiseSummaryReportRowDto>> GetProjectMonthwiseSummaryReport(
        GetProjectMonthwiseSummaryReportInput input)
    {
        input.Normalize();

        var period = ResolvePeriod(input.Month, input.Year);
        var query = await BuildQueryAsync(input, period);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);
        await FillPivotAsync(rows, period);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<ProjectMonthwiseSummaryReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetProjectMonthwiseSummaryReportToExcel(GetProjectMonthwiseSummaryReportInput input)
    {
        input.Normalize();

        var period = ResolvePeriod(input.Month, input.Year);
        var query = await BuildQueryAsync(input, period);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);
        var projects = await FillPivotAsync(rows, period);

        return await ExportToFileAsync(
            "ProjectMonthwiseSummary.xlsx",
            rows,
            GetExportColumns(projects),
            BuildParameterString(input));
    }

    /// <summary>
    /// Filtering + the scalar part of the projection (identity, Salary+Allowance). Ports legacy lines
    /// 495-550/612-653. The per-project pivot, OrgLevels and totals are stamped afterwards.
    /// </summary>
    private async Task<IQueryable<ProjectMonthwiseSummaryReportRowDto>> BuildQueryAsync(
        GetProjectMonthwiseSummaryReportInput input, DateTime period)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var payrollAreaCode = input.PayrollAreaCode;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var details = _projectRegisterDetailRepository.GetAll()
            .Where(d => d.ProjectRegister.Period == period)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.ProjectRegister.Employee.Code))
            .WhereIf(
                !string.IsNullOrWhiteSpace(payrollAreaCode),
                d => _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == d.ProjectRegister.EmployeeId
                                && o.StartDate <= period && o.EndDate >= period
                                && o.PayrollArea != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => o.PayrollArea.Code)
                    .FirstOrDefault() == payrollAreaCode)
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.ProjectRegister.EmployeeId));

        var employeeIds = details.Select(d => d.ProjectRegister.EmployeeId).Distinct();

        return employeeIds.Select(employeeId => new ProjectMonthwiseSummaryReportRowDto
        {
            Id = employeeId,
            EmployeeCode = details.Where(d => d.ProjectRegister.EmployeeId == employeeId).Select(d => d.ProjectRegister.Employee.Code).FirstOrDefault(),
            EmployeeName = details.Where(d => d.ProjectRegister.EmployeeId == employeeId)
                .Select(d => useArabicNames ? d.ProjectRegister.Employee.FullNameAr : d.ProjectRegister.Employee.FullNameEn)
                .FirstOrDefault(),
            // Legacy line 527: org assignment CURRENT AS OF TODAY, not the report period.
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == employeeId && o.StartDate.Date <= today && o.EndDate.Date >= today)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            JobTitle = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == employeeId && o.StartDate.Date <= today && o.EndDate.Date >= today && o.JobTitle != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                .FirstOrDefault(),
            // Legacy lines 541-549/651: sum of EmployeeBasicPayDetail.Amount current as of the period.
            SalaryAllowance = _employeeBasicPayDetailRepository.GetAll()
                .Where(b => b.EmployeeBasicPay.EmployeeId == employeeId
                            && b.EmployeeBasicPay.StartDate <= period && b.EmployeeBasicPay.EndDate >= period)
                .Sum(b => b.Amount),
            Salary = details.Where(d => d.ProjectRegister.EmployeeId == employeeId && d.WageType.BillingType == BillingType.Earning && !d.WageType.DontImpactNetPay).Sum(d => d.Amount)
                     - details.Where(d => d.ProjectRegister.EmployeeId == employeeId && d.WageType.BillingType == BillingType.Deduction && !d.WageType.DontImpactNetPay).Sum(d => d.Amount)
        });
    }

    /// <summary>Fills OrgLevel1..OrgLevel7 on already-materialized rows. See <see cref="EmployeeOrgLevelNameResolver"/>.</summary>
    private async Task StampOrgLevelsAsync(List<ProjectMonthwiseSummaryReportRowDto> rows)
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
    /// Stamps the per-project (WorkingDays, OTHours, Amount) pivot plus totals and OTAmount. Ports legacy
    /// lines 655-689.
    /// </summary>
    private async Task<List<ProjectColumn>> FillPivotAsync(List<ProjectMonthwiseSummaryReportRowDto> rows, DateTime period)
    {
        var useArabicNames = UseArabicNames;

        var projects = await _projectRegisterDetailRepository.GetAll()
            .Where(d => d.ProjectRegister.Period == period)
            .Select(d => new { d.ProjectId, d.Project.Code, NameEn = d.Project.NameEn, NameAr = d.Project.NameAr })
            .Distinct()
            .ToListAsync();

        var projectColumns = projects
            .OrderBy(p => useArabicNames ? p.NameAr : p.NameEn)
            .Select(p => new ProjectColumn { ProjectId = p.ProjectId, ProjectCode = p.Code, ProjectName = useArabicNames ? p.NameAr : p.NameEn })
            .ToList();

        if (rows.Count == 0 || projectColumns.Count == 0)
        {
            return projectColumns;
        }

        var otWageTypeIds = await _overtimeTypeRepository.GetAll()
            .Where(o => o.WageTypeId != null)
            .Select(o => o.WageTypeId.Value)
            .Distinct()
            .ToListAsync();

        var employeeIds = rows.Select(r => r.Id).ToList();
        var projectIds = projectColumns.Select(p => p.ProjectId).ToList();

        var details = await _projectRegisterDetailRepository.GetAll()
            .Where(d => d.ProjectRegister.Period == period
                        && employeeIds.Contains(d.ProjectRegister.EmployeeId)
                        && projectIds.Contains(d.ProjectId))
            .Select(d => new
            {
                EmployeeId = d.ProjectRegister.EmployeeId,
                d.ProjectId,
                WageTypeCode = d.WageType.Code,
                IsOvertime = otWageTypeIds.Contains(d.WageTypeId),
                IsEarning = d.WageType.BillingType == BillingType.Earning,
                IsDeduction = d.WageType.BillingType == BillingType.Deduction,
                DontImpactNetPay = d.WageType.DontImpactNetPay,
                d.Number,
                d.Amount
            })
            .ToListAsync();

        var detailsByEmployee = details.ToLookup(d => d.EmployeeId);

        foreach (var row in rows)
        {
            var rowDetails = detailsByEmployee[row.Id].ToList();

            row.OTAmount = rowDetails.Where(d => d.IsOvertime).Sum(d => d.Amount);

            row.ProjectAmounts = projectColumns.Select(p =>
            {
                var projectDetails = rowDetails.Where(d => d.ProjectId == p.ProjectId).ToList();

                return new ProjectMonthwiseAmountDto
                {
                    ProjectCode = p.ProjectCode,
                    ProjectName = p.ProjectName,
                    WorkingDays = projectDetails.Where(d => d.WageTypeCode == WorkingDaysWageTypeCode).Sum(d => d.Number),
                    OTHours = projectDetails.Where(d => d.IsOvertime).Sum(d => d.Number),
                    Amount = projectDetails.Where(d => d.IsEarning && !d.DontImpactNetPay).Sum(d => d.Amount)
                             - projectDetails.Where(d => d.IsDeduction && !d.DontImpactNetPay).Sum(d => d.Amount)
                };
            }).ToList();

            row.TotalWorkingDays = row.ProjectAmounts.Sum(p => p.WorkingDays);
            row.TotalOTHours = row.ProjectAmounts.Sum(p => p.OTHours);
            row.TotalAmount = row.ProjectAmounts.Sum(p => p.Amount);
        }

        return projectColumns;
    }

    private static DateTime ResolvePeriod(int month, int year)
    {
        var calendarEndDate = ResolveCalendarEnd(month, year);
        return calendarEndDate ?? FallbackPeriod;
    }

    private static DateTime? ResolveCalendarEnd(int month, int year)
    {
        // Legacy resolves this via PayrollCalendar (lines 422-436); this port keeps the same "last day of
        // the requested month" contract as the sibling ProjectCosting reports for consistency, since every
        // legacy fallback here already collapses to the 1900 sentinel when the calendar is missing.
        if (month < 1 || month > 12 || year < 1)
        {
            return null;
        }

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static IReadOnlyList<GridReportColumn<ProjectMonthwiseSummaryReportRowDto>> GetExportColumns(
        List<ProjectColumn> projects)
    {
        var columns = new List<GridReportColumn<ProjectMonthwiseSummaryReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitle),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("SalaryPlusAllowance", r => r.SalaryAllowance)
        };

        for (var i = 0; i < projects.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<ProjectMonthwiseSummaryReportRowDto>.FromLiteralHeader(
                projects[index].ProjectName + " WD", r => r.ProjectAmounts[index].WorkingDays));
        }

        columns.Add(new("TotalWorkingDays", r => r.TotalWorkingDays));

        for (var i = 0; i < projects.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<ProjectMonthwiseSummaryReportRowDto>.FromLiteralHeader(
                projects[index].ProjectName + " OT", r => r.ProjectAmounts[index].OTHours));
        }

        columns.Add(new("TotalOTHours", r => r.TotalOTHours));
        columns.Add(new("Salary", r => r.Salary));
        columns.Add(new("OTAmount", r => r.OTAmount));

        for (var i = 0; i < projects.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<ProjectMonthwiseSummaryReportRowDto>.FromLiteralHeader(
                projects[index].ProjectName + " AMT", r => r.ProjectAmounts[index].Amount));
        }

        columns.Add(new("TotalAmount", r => r.TotalAmount));

        return columns;
    }

    private class ProjectColumn
    {
        public int ProjectId { get; set; }

        public string ProjectCode { get; set; }

        public string ProjectName { get; set; }
    }
}
