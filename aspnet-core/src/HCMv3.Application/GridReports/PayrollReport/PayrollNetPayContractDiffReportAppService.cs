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
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Net Pay / Contract Diff grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollNetPayContractDiff.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 507-666).
///
/// NOT PORTED -- DevExpress/WebForms plumbing: drill-down session stack, ColumnChooser JSON, callback-panel
/// state machine, the dead <c>Table1_Old()</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollNetPayContractDiff)]
public class PayrollNetPayContractDiffReportAppService
    : GridReportAppServiceBase, IPayrollNetPayContractDiffReportAppService
{
    private const string NetPayWageTypeCode = "9000";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSponsor> _sponsorRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PayrollNetPayContractDiffReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSponsor> sponsorRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _sponsorRepository = sponsorRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPayrollNetPayContractDiff";

    protected override string ScreenCaption => "Payroll Net Pay Contract Diff";

    [HttpPost]
    public async Task<PagedResultDto<PayrollNetPayContractDiffReportRowDto>> GetPayrollNetPayContractDiffReport(
        GetPayrollNetPayContractDiffReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PayrollNetPayContractDiffReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollNetPayContractDiffReportToExcel(GetPayrollNetPayContractDiffReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollNetPayContractDiff.xlsx", ordered, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<PayrollNetPayContractDiffReportRowDto>> BuildRowsAsync(
        GetPayrollNetPayContractDiffReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var payrollRunType = input.PayrollRunType;

        var query = _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate && d.SalaryRegister.InPeriod == endDate)
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        if (input.PayrollAreaId.HasValue)
        {
            var payrollAreaId = input.PayrollAreaId.Value;
            query = query.Where(d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= endDate
                     && o.EndDate >= endDate
                     && o.PayrollAreaId == payrollAreaId));
        }

        var grouped = await query
            .GroupBy(d => new { d.SalaryRegister.EmployeeId, d.SalaryRegister.Employee.Code, d.SalaryRegister.Employee.FullNameEn, d.SalaryRegister.Employee.FullNameAr })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.Code,
                Name = useArabicNames ? g.Key.FullNameAr : g.Key.FullNameEn,
                NetPay = g.Where(d => d.WageType.Code == NetPayWageTypeCode).Select(d => (double?)d.Amount).FirstOrDefault() ?? 0
            })
            .ToListAsync();

        if (grouped.Count == 0)
        {
            return new List<PayrollNetPayContractDiffReportRowDto>();
        }

        var employeeIds = grouped.Select(g => g.EmployeeId).ToList();

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= Today && o.EndDate >= Today)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId, JobTitleNameEn = o.JobTitle.NameEn, JobTitleNameAr = o.JobTitle.NameAr })
            .ToListAsync();

        var orgAssignmentByEmployee = orgAssignments
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First());

        var sponsors = await _sponsorRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId) && s.StartDate.Date <= endDate && s.EndDate.Date >= endDate)
            .Select(s => new { s.EmployeeId, s.StartDate, s.Id, s.ContractSalary })
            .ToListAsync();

        var sponsorByEmployee = sponsors
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartDate).ThenBy(s => s.Id).First());

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        return grouped.OrderBy(g => g.Code).Select(g =>
        {
            orgAssignmentByEmployee.TryGetValue(g.EmployeeId, out var org);
            sponsorByEmployee.TryGetValue(g.EmployeeId, out var sponsor);
            var levels = lookup.GetLevelNames(org?.OrganizationUnitId);
            var contractPay = sponsor?.ContractSalary ?? 0;

            return new PayrollNetPayContractDiffReportRowDto
            {
                Id = g.EmployeeId,
                EmployeeCode = g.Code,
                EmployeeName = g.Name,
                JobTitle = org == null ? string.Empty : (useArabicNames ? org.JobTitleNameAr : org.JobTitleNameEn) ?? string.Empty,
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6],
                PayDate = endDate,
                ContractPay = contractPay,
                NetPay = g.NetPay,
                Difference = contractPay - g.NetPay
            };
        }).ToList();
    }

    private DateTime ResolveEndDate(GetPayrollNetPayContractDiffReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(1900, 1, 31);
        }
    }

    private string BuildParameterString(GetPayrollNetPayContractDiffReportInput input)
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

    private static IReadOnlyList<GridReportColumn<PayrollNetPayContractDiffReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PayrollNetPayContractDiffReportRowDto>>
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
            new("PayDate", r => r.PayDate),
            new("ContractPay", r => r.ContractPay),
            new("NetPay", r => r.NetPay),
            new("Difference", r => r.Difference)
        };
    }
}
