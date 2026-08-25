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
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Loan analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeLoan.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 499-683).
///
/// NOT PORTED: the drill-down <c>Table2()</c> (loan recovery detail on double-click), the ColumnChooser
/// JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeLoan)]
public class EmployeeLoanReportAppService : GridReportAppServiceBase, IEmployeeLoanReportAppService
{
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeLoanReportAppService(
        IRepository<EmployeeLoan> employeeLoanRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeLoanRepository = employeeLoanRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeLoan";

    protected override string ScreenCaption => "Employee Loan";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLoanReportRowDto>> GetEmployeeLoanReport(GetEmployeeLoanReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeLoanReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeLoanReportToExcel(GetEmployeeLoanReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var rows = await query.OrderBy(input.Sorting).ToListAsync();
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync("EmployeeLoan.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>Single source of truth for filtering AND projection. Ports legacy <c>Table1()</c> lines 579-641.</summary>
    private async Task<IQueryable<EmployeeLoanReportRowDto>> BuildQueryAsync(GetEmployeeLoanReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var loanTypeCode = input.LoanTypeCode;
        var today = Today;

        var query = _employeeLoanRepository.GetAll()
            // Legacy line 580: dtFromValue <= r.StartDate <= dtToValue.
            .Where(l => l.StartDate >= fromDate && l.StartDate <= toDate)
            .WhereIf(!loanTypeCode.IsNullOrWhiteSpace(), l => l.LoanType.Code == loanTypeCode)
            .WhereIf(employeeCodes.Count > 0, l => employeeCodes.Contains(l.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, l => rlsEmployeeIds.Contains(l.EmployeeId));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        return query.Select(l => new EmployeeLoanReportRowDto
        {
            Id = l.Id,
            EmployeeCode = l.Employee.Code,
            EmployeeName = useArabicNames ? l.Employee.FullNameAr : l.Employee.FullNameEn,
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == l.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == l.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            CountryName = currentOrgAssignments
                .Where(o => o.EmployeeId == l.EmployeeId && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null
                    ? null
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault(),
            LoanTypeName = l.LoanType == null ? string.Empty : (useArabicNames ? l.LoanType.NameAr : l.LoanType.NameEn),
            LoanPaymentDate = l.StartDate,
            RecoveryEndDate = l.EndDate,
            RecoveryStartDate = l.RecoveryStartDate,
            FixedAmount = l.FixedAmount,
            DueAmount = l.DueAmount,
            BalanceDueAmount = l.BalanceDueAmount,
            RecoveryAmount = l.DueAmount - l.BalanceDueAmount,
            Comments = l.Comments,
            // Legacy line 671: filled from the latest separation's SubmissionDate, not LastDayInService.
            LastDayInService = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == l.EmployeeId)
                .OrderByDescending(s => s.SubmissionDate)
                .Select(s => (DateTime?)s.SubmissionDate)
                .FirstOrDefault()
        });
    }

    private async Task StampOrgLevelsAsync(List<EmployeeLoanReportRowDto> rows)
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

    private string BuildParameterString(GetEmployeeLoanReportInput input)
    {
        var parts = new List<string>
        {
            "From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"),
            "To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy")
        };

        if (!input.LoanTypeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Loan Type : " + input.LoanTypeCode);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<EmployeeLoanReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeLoanReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("LoanType", r => r.LoanTypeName),
            new("LoanPaymentDate", r => r.LoanPaymentDate),
            new("RecoveryStartDate", r => r.RecoveryStartDate),
            new("RecoveryEndDate", r => r.RecoveryEndDate),
            new("FixedAmount", r => r.FixedAmount),
            new("DueAmount", r => r.DueAmount),
            new("RecoveryAmount", r => r.RecoveryAmount),
            new("BalanceDueAmount", r => r.BalanceDueAmount),
            new("Comments", r => r.Comments),
            new("Country", r => r.CountryName),
            new("LastDayInService", r => r.LastDayInService)
        };
    }
}
