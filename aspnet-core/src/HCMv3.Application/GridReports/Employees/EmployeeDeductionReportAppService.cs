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
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Deduction analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeDeduction.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 552-739).
///
/// ATYPICAL FILTERS: this report's panel is Month / Year / Run Type / Payroll Area, not the family's usual
/// From-To/EmployeeCode/EmployeeStatus. Month+Year are turned into the LAST DAY of that month, and both
/// <c>SalaryRegister.ForPeriod</c> AND <c>InPeriod</c> must equal it (legacy lines 490-501 and 645-649),
/// i.e. only the regular in-period run for that month is reported, not retro runs.
///
/// NOT PORTED (DevExpress/WebForms plumbing): legacy's <c>GetKey()</c> advertises a level-2 "OID" drill-down
/// (line 462) but <c>GetTable()</c> has no <c>case 2</c> (lines 500-506) and there is no <c>Table2()</c> in
/// the file -- the drill-down is dead code. Also not ported: the ColumnChooser JSON persistence, the
/// callback-panel state machine, the PDF export button, and the dead <c>Table1_Old()</c> (lines 740+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeDeduction)]
public class EmployeeDeductionReportAppService : GridReportAppServiceBase, IEmployeeDeductionReportAppService
{
    /// <summary>Legacy lines 638-642 / 683-690.</summary>
    private const string LateEarlyWageTypeCode = "2000";

    private const string LoanRecoveryWageTypeCode = "2001";

    private const string UnpaidLeaveWageTypeCode = "2007";

    private const string LateEarlyAfterToleranceWageTypeCode = "2010";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeDeductionReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeDeduction";

    protected override string ScreenCaption => "Employee Deduction";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDeductionReportRowDto>> GetEmployeeDeductionReport(
        GetEmployeeDeductionReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgAssignmentColumnsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildDeductionParameterString(input), totalCount);

        return new PagedResultDto<EmployeeDeductionReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeDeductionReportToExcel(GetEmployeeDeductionReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgAssignmentColumnsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeDeduction.xlsx",
            rows,
            GetExportColumns(),
            BuildDeductionParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND aggregation, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s data region (lines 636-696).
    ///
    /// The JobTitle / Payroll Area / OrgLevel columns are NOT part of this query; they are per-employee
    /// lookups against the CURRENT org assignment and are stamped on afterwards by
    /// <see cref="StampOrgAssignmentColumnsAsync"/>, which both entry points also call.
    /// </summary>
    private async Task<IQueryable<EmployeeDeductionReportRowDto>> BuildQueryAsync(
        GetEmployeeDeductionReportInput input)
    {
        var periodEndDate = ResolvePeriodEndDate(input);
        var payrollRunType = input.PayrollRunType;
        var payrollAreaId = input.PayrollAreaId;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var query = _salaryRegisterDetailRepository.GetAll()
            // Legacy lines 638-642.
            .Where(d => d.SalaryRegister.ForPeriod == periodEndDate
                        && d.SalaryRegister.InPeriod == periodEndDate
                        && (d.WageType.Code == LateEarlyWageTypeCode
                            || d.WageType.Code == LoanRecoveryWageTypeCode
                            || d.WageType.Code == UnpaidLeaveWageTypeCode
                            || d.WageType.Code == LateEarlyAfterToleranceWageTypeCode))
            // Legacy lines 645-649.
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            // Legacy lines 651-659: the employee must have an org assignment CURRENT AS OF TODAY on the
            // selected payroll area (note: today, not the reported period -- legacy used DateTime.Now).
            .WhereIf(payrollAreaId.HasValue, d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= today
                     && o.EndDate >= today
                     && o.PayrollAreaId == payrollAreaId.Value))
            // Legacy line 661 ApplyRowLevelSecurity -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 674-691: one row per employee, each column a conditional SUM over a wage type.
        return query
            .GroupBy(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.Employee.Code,
                d.SalaryRegister.Employee.FullNameEn,
                d.SalaryRegister.Employee.FullNameAr
            })
            .Select(g => new EmployeeDeductionReportRowDto
            {
                // Legacy "OID" column (line 700).
                Id = g.Key.EmployeeId,
                EmployeeCode = g.Key.Code,
                EmployeeName = useArabicNames ? g.Key.FullNameAr : g.Key.FullNameEn,
                LateEarlyDeduction = g.Sum(d => d.WageType.Code == LateEarlyWageTypeCode ? d.Amount : 0),
                LateEarlyMinutes = g.Sum(d => d.WageType.Code == LateEarlyWageTypeCode ? d.Number : 0),
                UnpaidLeaveDeduction = g.Sum(d => d.WageType.Code == UnpaidLeaveWageTypeCode ? d.Amount : 0),
                UnpaidLeaveDays = g.Sum(d => d.WageType.Code == UnpaidLeaveWageTypeCode ? d.Number : 0),
                LoanRecoveryDeduction = g.Sum(d => d.WageType.Code == LoanRecoveryWageTypeCode ? d.Amount : 0),
                LoanRecoveryDays = g.Sum(d => d.WageType.Code == LoanRecoveryWageTypeCode ? d.Number : 0),
                LateEarlyAfterMonthlyTolerance =
                    g.Sum(d => d.WageType.Code == LateEarlyAfterToleranceWageTypeCode ? d.Amount : 0),
                LateEarlyAfterMonthlyToleranceDays =
                    g.Sum(d => d.WageType.Code == LateEarlyAfterToleranceWageTypeCode ? d.Number : 0)
            });
    }

    /// <summary>
    /// Legacy lines 481-501: Month/Year (defaulting to the current month/year when blank) become the LAST
    /// day of that month. Legacy fell back to 1900-01-31 when the two values did not form a valid date;
    /// the DTO's Range attributes make that unreachable here, but the same fallback is kept for safety.
    /// </summary>
    private DateTime ResolvePeriodEndDate(GetEmployeeDeductionReportInput input)
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

    /// <summary>
    /// Fills JobTitle, Payroll Area and OrgLevel1..7 from the org assignment CURRENT AS OF TODAY -- legacy
    /// lines 663-671 (<c>empOrgAss</c>) and 705-717. Shared by the paged read and the export.
    /// </summary>
    private async Task StampOrgAssignmentColumnsAsync(List<EmployeeDeductionReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var today = Today;
        var useArabicNames = UseArabicNames;
        var employeeIds = rows.Select(r => r.Id).ToList();

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= today && o.EndDate >= today)
            .Select(o => new
            {
                o.EmployeeId,
                o.StartDate,
                o.Id,
                o.OrganizationUnitId,
                PayrollAreaName = o.PayrollArea == null ? string.Empty : o.PayrollArea.Name,
                JobTitle = o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
            })
            .ToListAsync();

        var byEmployee = assignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StartDate).ThenBy(a => a.Id).First());

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        foreach (var row in rows)
        {
            if (byEmployee.TryGetValue(row.Id, out var assignment))
            {
                row.JobTitle = assignment.JobTitle;
                row.PayrollAreaName = assignment.PayrollAreaName;
                row.OrganizationUnitId = assignment.OrganizationUnitId;
            }
            else
            {
                // Legacy line 705: with no current assignment the org columns simply stay blank.
                row.JobTitle = string.Empty;
                row.PayrollAreaName = string.Empty;
            }

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
    /// Ports legacy <c>GetParameterStr</c> (lines 534-548), which logs Payroll Area / Run Type / Month /
    /// Year -- NOT the base class's Status/Code/From/To, none of which this screen has.
    /// </summary>
    private string BuildDeductionParameterString(GetEmployeeDeductionReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }

    /// <summary>The exported column set, in legacy column order. The technical OID key is omitted.</summary>
    private static IReadOnlyList<GridReportColumn<EmployeeDeductionReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeDeductionReportRowDto>>
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
            new("PayrollArea", r => r.PayrollAreaName),
            new("LateEarlyDeduction", r => r.LateEarlyDeduction),
            new("LateEarlyMinutes", r => r.LateEarlyMinutes),
            new("UnpaidLeaveDeduction", r => r.UnpaidLeaveDeduction),
            new("UnpaidLeaveDays", r => r.UnpaidLeaveDays),
            new("LoanRecoveryDeduction", r => r.LoanRecoveryDeduction),
            new("LoanRecoveryDays", r => r.LoanRecoveryDays),
            new("LateEarlyDeductionAfterMonthlyTolerance", r => r.LateEarlyAfterMonthlyTolerance),
            new("LateEarlyAfterMonthlyToleranceDays", r => r.LateEarlyAfterMonthlyToleranceDays)
        };
    }
}
