using System;
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
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Service Bonus grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptServiceBonus.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 497-710).
///
/// LEGACY QUIRK REPRODUCED: the date-range filter is displayed and logged but never applied -- see
/// <see cref="GetServiceBonusReportInput"/> remarks.
///
/// A row is emitted only when the employee has EITHER a wage-35 OR a wage-36 register line (legacy's
/// nested <c>if</c>/<c>else if</c> around <c>table.Rows.Add</c>, lines 667-698); an employee with neither
/// contributes no row at all.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ServiceBonus)]
public class ServiceBonusReportAppService : GridReportAppServiceBase, IServiceBonusReportAppService
{
    private const string ServiceBonusWageTypeCode = "35";
    private const string ServiceBonusArrearWageTypeCode = "36";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public ServiceBonusReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptServiceBonus";

    protected override string ScreenCaption => "Service Bonus";

    [HttpPost]
    public async Task<PagedResultDto<ServiceBonusReportRowDto>> GetServiceBonusReport(GetServiceBonusReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<ServiceBonusReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetServiceBonusReportToExcel(GetServiceBonusReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("ServiceBonus.xlsx", ordered, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<ServiceBonusReportRowDto>> BuildRowsAsync(GetServiceBonusReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        var employees = await _employeeRepository.GetAll()
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Code, NameEn = e.FullNameEn, NameAr = e.FullNameAr, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();

        if (employees.Count == 0)
        {
            return new List<ServiceBonusReportRowDto>();
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        // Legacy lines 587-611: FirstOrDefault of the employee's Regular in-period register lines for
        // wage 35/36 -- no explicit ordering, so "any one" is legacy-faithful; the first by SalaryRegister
        // id is used here for determinism.
        var bonusLines = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                        && d.SalaryRegister.InPeriod == d.SalaryRegister.ForPeriod
                        && d.SalaryRegister.PayrollRunType == PayrollRunType.Regular
                        && (d.WageType.Code == ServiceBonusWageTypeCode || d.WageType.Code == ServiceBonusArrearWageTypeCode))
            .Select(d => new { d.SalaryRegister.EmployeeId, d.SalaryRegisterId, d.WageType.Code, d.SalaryRegister.ForPeriod, d.Amount })
            .ToListAsync();

        var bonusByEmployee = bonusLines
            .Where(l => l.Code == ServiceBonusWageTypeCode)
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(l => l.SalaryRegisterId).First());

        var arrearByEmployee = bonusLines
            .Where(l => l.Code == ServiceBonusArrearWageTypeCode)
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(l => l.SalaryRegisterId).First());

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= Today && o.EndDate >= Today)
            .Select(o => new
            {
                o.EmployeeId,
                o.StartDate,
                o.Id,
                o.OrganizationUnitId,
                JobTitleNameEn = o.JobTitle.NameEn,
                JobTitleNameAr = o.JobTitle.NameAr,
                GradeNameEn = o.EmployeeSubGroup.NameEn,
                GradeNameAr = o.EmployeeSubGroup.NameAr
            })
            .ToListAsync();

        var orgAssignmentByEmployee = orgAssignments
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First());

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var rows = new List<ServiceBonusReportRowDto>();

        foreach (var e in employees)
        {
            bonusByEmployee.TryGetValue(e.Id, out var bonus);
            arrearByEmployee.TryGetValue(e.Id, out var arrear);

            // Legacy 667-698: a row is emitted only when bonus OR arrear exists.
            if (bonus == null && arrear == null)
            {
                continue;
            }

            orgAssignmentByEmployee.TryGetValue(e.Id, out var org);
            var levels = lookup.GetLevelNames(org?.OrganizationUnitId);

            var dueDate = bonus?.ForPeriod ?? arrear?.ForPeriod;
            double? servicePeriod = null;
            if (dueDate.HasValue)
            {
                var span = dueDate.Value.AddDays(1) - e.JoiningDate;
                var zero = new DateTime(1, 1, 1);
                servicePeriod = (zero + span).Year - 1;
            }

            rows.Add(new ServiceBonusReportRowDto
            {
                Id = e.Id,
                EmployeeCode = e.Code,
                EmployeeName = e.NameEn,
                EmployeeNameAr = e.NameAr,
                JobTitle = org == null ? string.Empty : (useArabicNames ? org.JobTitleNameAr : org.JobTitleNameEn) ?? string.Empty,
                Grade = org == null ? string.Empty : (useArabicNames ? org.GradeNameAr : org.GradeNameEn) ?? string.Empty,
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6],
                JoiningDate = e.JoiningDate,
                ServiceDueDate = dueDate,
                ServicePeriod = servicePeriod,
                Amount = bonus?.Amount ?? 0,
                ArrearAmount = arrear?.Amount ?? 0
            });
        }

        return rows;
    }

    private string BuildParameterString(GetServiceBonusReportInput input)
    {
        var parts = new List<string>();

        if (input.EmployeeStatus.HasValue)
        {
            parts.Add("Status Type : " + input.EmployeeStatus.Value);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        parts.Add("From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"));
        parts.Add("To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy"));

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<ServiceBonusReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<ServiceBonusReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("FullNameAr", r => r.EmployeeNameAr),
            new("JobTitle", r => r.JobTitle),
            new("Grade", r => r.Grade),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("JoiningDate", r => r.JoiningDate),
            new("ServiceDueDate", r => r.ServiceDueDate),
            new("ServicePeriod", r => r.ServicePeriod),
            new("Amount", r => r.Amount),
            new("ArrearAmount", r => r.ArrearAmount)
        };
    }
}
