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
using HCMv3.GridReports.Status.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee New Joiner analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeNewJoiner.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 460-706).
///
/// See <see cref="Dto.EmployeeNewJoinerReportRowDto"/> for the one deliberate scope reduction (the
/// data-driven per-wage-type pivot columns are not reproduced; the aggregate Salary/Currency are).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeNewJoiner)]
public class EmployeeNewJoinerReportAppService : GridReportAppServiceBase, IEmployeeNewJoinerReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeNewJoinerReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeContractRepository = employeeContractRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeNewJoiner";

    protected override string ScreenCaption => "Employee New Joiner";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeNewJoinerReportRowDto>> GetEmployeeNewJoinerReport(
        GetEmployeeNewJoinerReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeNewJoinerReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeNewJoinerReportToExcel(GetEmployeeNewJoinerReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeNewJoiner.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeNewJoiner.ascx.cs:460-706).
    /// </summary>
    private async Task<IQueryable<EmployeeNewJoinerReportRowDto>> BuildQueryAsync(
        GetEmployeeNewJoinerReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var today = Today;

        var query = _employeeRepository.GetAll()
            // Legacy lines 574-575.
            .Where(e => e.JoiningDate >= fromDate && e.JoiningDate <= toDate)
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        var basicPayDetailsCurrent = _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate.Date <= today
                        && d.EmployeeBasicPay.EndDate.Date >= today
                        && d.WageType != null);

        // Legacy line 636: most recent contract by StartDate, no date-window restriction.
        var latestContracts = _employeeContractRepository.GetAll();

        return query.Select(e => new EmployeeNewJoinerReportRowDto
        {
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn,
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            CountryName = e.Country == null ? string.Empty : (useArabicNames ? e.Country.NameAr : e.Country.NameEn),
            JoiningDate = e.JoiningDate,
            Salary = basicPayDetailsCurrent.Where(d => d.EmployeeBasicPay.EmployeeId == e.Id).Sum(d => (double?)d.Amount) ?? 0,
            CurrencyName = basicPayDetailsCurrent
                .Where(d => d.EmployeeBasicPay.EmployeeId == e.Id && d.Currency != null)
                .OrderBy(d => d.Id)
                .Select(d => useArabicNames ? d.Currency.CurrencyShortFormAr : d.Currency.CurrencyShortFormEn)
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? string.Empty : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            Gender = e.Gender,
            NationalityName = e.Nationality == null ? string.Empty : (useArabicNames ? e.Nationality.NameAr : e.Nationality.NameEn),
            Email = e.Email,
            Mobile = e.Mobile,
            ContractTypeName = latestContracts
                .Where(c => c.EmployeeId == e.Id)
                .OrderByDescending(c => c.StartDate).ThenByDescending(c => c.Id)
                .Select(c => c.ContractType == null ? string.Empty : (useArabicNames ? c.ContractType.NameAr : c.ContractType.NameEn))
                .FirstOrDefault(),
            ContractStartDate = latestContracts
                .Where(c => c.EmployeeId == e.Id)
                .OrderByDescending(c => c.StartDate).ThenByDescending(c => c.Id)
                .Select(c => (DateTime?)c.StartDate)
                .FirstOrDefault(),
            ContractEndDate = latestContracts
                .Where(c => c.EmployeeId == e.Id)
                .OrderByDescending(c => c.StartDate).ThenByDescending(c => c.Id)
                .Select(c => (DateTime?)c.EndDate)
                .FirstOrDefault(),
            CostCenterName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null ? string.Empty : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            CostCenterGroupName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id && o.CostCenter != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter.CostCenterGroup == null ? string.Empty : (useArabicNames ? o.CostCenter.CostCenterGroup.NameAr : o.CostCenter.CostCenterGroup.NameEn))
                .FirstOrDefault()
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeNewJoinerReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeNewJoinerReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("Country", r => r.CountryName),
            new("JoiningDate", r => r.JoiningDate),
            new("Salary", r => r.Salary),
            new("Currency", r => r.CurrencyName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocationName),
            new("Gender", r => r.Gender.ToString()),
            new("Nationality", r => r.NationalityName),
            new("Email", r => r.Email),
            new("Mobile", r => r.Mobile),
            new("ContractType", r => r.ContractTypeName),
            new("ContractStartDate", r => r.ContractStartDate),
            new("ContractEndDate", r => r.ContractEndDate),
            new("CostCenter", r => r.CostCenterName),
            new("CostCenterGroup", r => r.CostCenterGroupName)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeNewJoinerReportRowDto> rows)
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
