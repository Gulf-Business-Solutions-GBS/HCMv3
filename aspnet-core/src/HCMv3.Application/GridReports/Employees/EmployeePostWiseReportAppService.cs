using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using HCMv3.AccommodationManagement;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Post Wise analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeePostWise.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 465-696).
///
/// DATE-LESS report: legacy hides both date pickers and <c>Table1()</c> never reads them -- every lookup
/// in it is keyed on <c>DateTime.Now</c>. The inherited From/To filters are accepted and ignored.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (its <c>GetTable()</c> even keeps the Table2/Table3 drill-down cases COMMENTED OUT, lines
/// 421-428, so the screen really is single-level); the ColumnChooser JSON persistence; the callback-panel
/// state machine; the PDF export button; and the dead <c>Table1_Old()</c> (lines 698+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeePostWise)]
public class EmployeePostWiseReportAppService : GridReportAppServiceBase, IEmployeePostWiseReportAppService
{
    private const string BasicPayWageTypeCode = "1000";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeAccommodation> _employeeAccommodationRepository;
    private readonly IRepository<AccommodationServices> _accommodationServicesRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeePostWiseReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeAccommodation> employeeAccommodationRepository,
        IRepository<AccommodationServices> accommodationServicesRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeAccommodationRepository = employeeAccommodationRepository;
        _accommodationServicesRepository = accommodationServicesRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeePostWise";

    protected override string ScreenCaption => "Employee Post Wise";

    [HttpPost]
    public async Task<PagedResultDto<EmployeePostWiseReportRowDto>> GetEmployeePostWiseReport(
        GetEmployeePostWiseReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        ApplyComputedColumns(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeePostWiseReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeePostWiseReportToExcel(GetEmployeePostWiseReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        ApplyComputedColumns(rows);

        return await ExportToFileAsync(
            "EmployeePostWise.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeePostWise.ascx.cs:465-696).
    /// </summary>
    private async Task<IQueryable<EmployeePostWiseReportRowDto>> BuildQueryAsync(
        GetEmployeePostWiseReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Every date-effective lookup in this report is keyed on today -- legacy used DateTime.Now
        // throughout and never read its (hidden) date pickers.
        var today = Today;

        var query = _employeeRepository.GetAll()
            // Legacy lines 537-541: Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            // Legacy lines 542-546: employees.Contains(Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy line 548 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        // Legacy lines 550-566: wage detail rows of the basic-pay record current today.
        var payDetailsToday = _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= today && d.EmployeeBasicPay.EndDate >= today);

        // Legacy lines 569-583: the org assignment current today.
        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        // Legacy lines 606-610: accommodation current today; 612-621: its services, filtered on their own dates.
        var accommodationsToday = _employeeAccommodationRepository.GetAll()
            .Where(a => a.StartDate <= today && a.EndDate >= today);

        var accommodationServicesToday = _accommodationServicesRepository.GetAll()
            .Where(s => s.StartDate <= today && s.EndDate >= today);

        return query.Select(e => new EmployeePostWiseReportRowDto
        {
            // Legacy "OID" column -- the row key, not a data column.
            Id = e.Id,
            EmployeeCode = e.Code,
            // Legacy line 634 uses FullNameEn unconditionally on this screen.
            EmployeeName = e.FullNameEn,
            Status = e.Status,
            JoiningDate = e.JoiningDate,
            BirthDate = e.BirthDate,
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            // Legacy line 671: FirstOrDefault of wage type 1000 -- NOT a sum.
            BasicSalary = payDetailsToday
                .Where(d => d.EmployeeBasicPay.EmployeeId == e.Id && d.WageType.Code == BasicPayWageTypeCode)
                .OrderBy(d => d.Id)
                .Select(d => d.Amount)
                .FirstOrDefault(),
            // Legacy line 672: every other wage type flagged IncludeInBasicPay.
            Allowance = payDetailsToday
                .Where(d => d.EmployeeBasicPay.EmployeeId == e.Id
                            && d.WageType.Code != BasicPayWageTypeCode
                            && d.WageType.IncludeInBasicPay)
                .Sum(d => d.Amount),
            // Legacy line 673: the sum of ALL wage-type amounts.
            TotalSalary = payDetailsToday
                .Where(d => d.EmployeeBasicPay.EmployeeId == e.Id)
                .Sum(d => d.Amount),
            // Legacy lines 600-604 + 665: latest LastDayInService of a separation that was never revoked.
            LastWorkingDay = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.RevokeDate == null)
                .OrderByDescending(s => s.LastDayInService)
                .Select(s => (DateTime?)s.LastDayInService)
                .FirstOrDefault(),
            AccommodationTypeName = accommodationsToday
                .Where(a => a.EmployeeId == e.Id)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.AccommodationType == null ? string.Empty : a.AccommodationType.NameEn)
                .FirstOrDefault(),
            TransportationTypeName = accommodationServicesToday
                .Where(s => s.EmployeeAccommodation.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.AccommodationServiceType == null ? string.Empty : s.AccommodationServiceType.NameEn)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Stamps the two "as of now" derived columns onto already-materialized rows. Called by BOTH the read
    /// and the export so they cannot drift. Legacy computed them inline while filling the DataTable
    /// (Age line 638, ServiceDuration lines 677-679); neither has a faithful SQL translation.
    /// </summary>
    private void ApplyComputedColumns(List<EmployeePostWiseReportRowDto> rows)
    {
        var now = Clock.Now;

        foreach (var row in rows)
        {
            // Legacy line 638: whole years + (month delta, minus one when the day-of-month has not been
            // reached) / 12, rounded to one decimal.
            row.Age = Math.Round(
                (now.Year - row.BirthDate.Year) +
                ((now.Month - row.BirthDate.Month) + (now.Day >= row.BirthDate.Day ? 0 : -1)) / 12.0,
                1);

            // Legacy lines 677-679: measured to the last working day when there is one, else to now.
            var endOfService = row.LastWorkingDay ?? now;
            row.ServiceDurationYears = Math.Round((endOfService - row.JoiningDate).TotalDays / 365.25, 1);
        }
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted; OrgLevel1..7 is
    /// the single OrganizationUnit column. BirthDate is exported too -- it is the input to the Age column
    /// and legacy's Age-only sheet made that number impossible to verify.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeePostWiseReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeePostWiseReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("Status", r => r.Status.ToString()),
            new("JoiningDate", r => r.JoiningDate),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("BasicSalary", r => r.BasicSalary),
            new("Allowance", r => r.Allowance),
            new("TotalSalary", r => r.TotalSalary),
            new("AccommodationType", r => r.AccommodationTypeName),
            new("TransportationType", r => r.TransportationTypeName),
            new("ServiceDuration", r => r.ServiceDurationYears),
            new("LastWorkingDay", r => r.LastWorkingDay),
            new("BirthDate", r => r.BirthDate),
            new("Age", r => r.Age)
        };
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeePostWiseReportRowDto> rows)
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
