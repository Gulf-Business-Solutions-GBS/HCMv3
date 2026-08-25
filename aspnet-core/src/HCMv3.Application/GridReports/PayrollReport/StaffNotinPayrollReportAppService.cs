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
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Staff Not in Payroll grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffNotinPayroll.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 498-625): every <c>EmployeeOnHold</c> record whose window overlaps the reported
/// month, with <c>OnHold == true</c>.
///
/// NOT PORTED -- DevExpress/WebForms plumbing: drill-down session stack, ColumnChooser JSON, callback-panel
/// state machine.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_StaffNotinPayroll)]
public class StaffNotinPayrollReportAppService : GridReportAppServiceBase, IStaffNotinPayrollReportAppService
{
    private readonly IRepository<EmployeeOnHold> _onHoldRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public StaffNotinPayrollReportAppService(
        IRepository<EmployeeOnHold> onHoldRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _onHoldRepository = onHoldRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptStaffNotinPayroll";

    protected override string ScreenCaption => "Staff Not in Payroll";

    [HttpPost]
    public async Task<PagedResultDto<StaffNotinPayrollReportRowDto>> GetStaffNotinPayrollReport(
        GetStaffNotinPayrollReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<StaffNotinPayrollReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetStaffNotinPayrollReportToExcel(GetStaffNotinPayrollReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "StaffNotinPayroll.xlsx", ordered, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<StaffNotinPayrollReportRowDto>> BuildRowsAsync(GetStaffNotinPayrollReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var startDate = new DateTime(endDate.Year, endDate.Month, 1);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var query = _onHoldRepository.GetAll()
            // Legacy line 552: overlap of the on-hold window with [StartDate, EndDate], plus OnHold flag.
            .Where(h => !(h.StartDate.Date > endDate || h.EndDate.Date < startDate) && h.OnHold)
            .WhereIf(rlsEmployeeIds.Count > 0, h => rlsEmployeeIds.Contains(h.EmployeeId));

        if (input.PayrollAreaId.HasValue)
        {
            var payrollAreaId = input.PayrollAreaId.Value;
            query = query.Where(h => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == h.EmployeeId
                     && o.StartDate <= endDate
                     && o.EndDate >= endDate
                     && o.PayrollAreaId == payrollAreaId));
        }

        var onHoldRows = await query
            .Select(h => new
            {
                h.EmployeeId,
                Code = h.Employee.Code,
                Name = useArabicNames ? h.Employee.FullNameAr : h.Employee.FullNameEn,
                h.Employee.JoiningDate
            })
            .Distinct()
            .OrderBy(r => r.Code)
            .ToListAsync();

        if (onHoldRows.Count == 0)
        {
            return new List<StaffNotinPayrollReportRowDto>();
        }

        var employeeIds = onHoldRows.Select(r => r.EmployeeId).Distinct().ToList();

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= endDate && o.EndDate >= endDate)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId, o.JobTitleId, JobTitleNameEn = o.JobTitle.NameEn, JobTitleNameAr = o.JobTitle.NameAr, PayrollAreaName = o.PayrollArea.Name })
            .ToListAsync();

        var orgAssignmentByEmployee = orgAssignments
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First());

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        return onHoldRows.Select(r =>
        {
            orgAssignmentByEmployee.TryGetValue(r.EmployeeId, out var org);
            var levels = lookup.GetLevelNames(org?.OrganizationUnitId);

            return new StaffNotinPayrollReportRowDto
            {
                Id = r.EmployeeId,
                EmployeeCode = r.Code,
                EmployeeName = r.Name,
                JoiningDate = r.JoiningDate,
                JobTitle = org == null ? string.Empty : (useArabicNames ? org.JobTitleNameAr : org.JobTitleNameEn) ?? string.Empty,
                PayrollAreaName = org?.PayrollAreaName ?? string.Empty,
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6]
            };
        }).ToList();
    }

    private DateTime ResolveEndDate(GetStaffNotinPayrollReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return Today;
        }
    }

    private string BuildParameterString(GetStaffNotinPayrollReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<StaffNotinPayrollReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<StaffNotinPayrollReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JoiningDate", r => r.JoiningDate),
            new("PayrollArea", r => r.PayrollAreaName),
            new("JobTitle", r => r.JobTitle),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7)
        };
    }
}
