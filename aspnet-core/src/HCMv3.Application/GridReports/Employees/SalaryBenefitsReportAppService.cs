using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Timing;
using Abp.Linq.Extensions;
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
/// Salary Benefits analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptSalaryBenefits.ascx(.cs)</c>.
///
/// Ports <c>Table1()</c> (lines 514-885). The row source is <see cref="Employee"/> itself; every
/// date-effective lookup is pinned to TODAY (legacy's local <c>EndDate = DateTime.Now.Date</c>,
/// line 518) -- this report has no date window even though the shared filter panel offers one.
///
/// ATYPICAL: THE COLUMN SET IS DYNAMIC -- one column per wage type present in any basic pay current
/// today. See <see cref="SalaryBenefitsReportRowDto.WageAmounts"/> and
/// <see cref="SalaryBenefitsReportOutput.WageTypeNames"/>.
///
/// TWO-PHASE MATERIALIZATION: <see cref="BuildQueryAsync"/> does ALL filtering and every SQL-expressible
/// column, and <see cref="PopulateDerivedValuesAsync"/> then fills the three things that cannot be
/// expressed in a translatable projection -- the per-wage-type amounts (a pivot over a variable column
/// list), <c>ServiceDuration</c> and <c>Age</c> (both day-level date arithmetic). BOTH the paged read
/// and the export call BOTH phases in the same order, so the export still honours exactly the filters
/// the grid shows; the split is about SQL translatability, not about filtering.
///
/// DEVIATIONS from legacy, all detailed on <see cref="SalaryBenefitsReportRowDto"/>: OrgLevel1..7 are
/// resolved from the org unit tree; the "RevokeComments" column is dropped (no such property on HCMv3's
/// <c>EmployeeSeparation</c>).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing: the drill-down "Table2"/"Table3" branches (commented
/// out in legacy's own <c>GetTable()</c>, lines 460-467, so dead in production), the ColumnChooser JSON
/// persistence, the callback-panel state machine, and the PDF export button. Also not ported: the large
/// block of anonymous-projection fields legacy computes but never writes into any column (bank account,
/// documents, addresses, contract dates, employee group/sub group, ... lines 700-795) -- they are
/// leftovers from the report this one was copied from and have no column to land in.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SalaryBenefits)]
public class SalaryBenefitsReportAppService : GridReportAppServiceBase, ISalaryBenefitsReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeAccommodation> _employeeAccommodationRepository;
    private readonly IRepository<AccommodationServices> _accommodationServicesRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public SalaryBenefitsReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeAccommodation> employeeAccommodationRepository,
        IRepository<AccommodationServices> accommodationServicesRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeAccommodationRepository = employeeAccommodationRepository;
        _accommodationServicesRepository = accommodationServicesRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptSalaryBenefits";

    protected override string ScreenCaption => "Salary Benefits";

    [HttpPost]
    public async Task<SalaryBenefitsReportOutput> GetSalaryBenefitsReport(GetSalaryBenefitsReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        var wageTypeNames = await PopulateDerivedValuesAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildParameterStringForReport(input),
            totalCount);

        return new SalaryBenefitsReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            WageTypeNames = wageTypeNames
        };
    }

    [HttpPost]
    public async Task<FileDto> GetSalaryBenefitsReportToExcel(GetSalaryBenefitsReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        var wageTypeNames = await PopulateDerivedValuesAsync(rows);

        return await ExportToFileAsync(
            "SalaryBenefits.xlsx",
            rows,
            GetExportColumns(wageTypeNames),
            BuildParameterStringForReport(input));
    }

    /// <summary>
    /// Legacy prepends the payroll-area filter to the logged parameter string
    /// (uscRptSalaryBenefits.ascx.cs:495-497). The report has no date inputs, so the common
    /// From/To parts the base class adds are still emitted (they resolve to today) but mean nothing here.
    /// </summary>
    private string BuildParameterStringForReport(GetSalaryBenefitsReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND the SQL-expressible part of the projection, shared by the
    /// paged read and the export. Ports legacy <c>Table1()</c>'s filter block (lines 518-548) and its
    /// per-column lookups.
    /// </summary>
    private async Task<IQueryable<SalaryBenefitsReportRowDto>> BuildQueryAsync(
        GetSalaryBenefitsReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;
        var payrollAreaCode = input.PayrollAreaCode;
        var today = Today;

        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        var assignments = _employeeOrgAssignmentRepository.GetAll();

        // Legacy's "current" org assignment: the one covering today (line 543-546 / the Employee entity's
        // CurrentOrgAssignment).
        var currentAssignments = assignments.Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        var query = _employeeRepository.GetAll()
            // Legacy lines 524-529.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            // Legacy lines 533-537.
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy lines 539-547: the report-specific cmbPayrollArea, matched on the CODE against the
            // employee's org assignment current today.
            .WhereIf(!payrollAreaCode.IsNullOrWhiteSpace(),
                e => currentAssignments.Any(o => o.EmployeeId == e.Id
                                                 && o.PayrollArea != null
                                                 && o.PayrollArea.Code == payrollAreaCode))
            // Legacy line 549 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var basicPays = _employeeBasicPayRepository.GetAll()
            .Where(p => p.StartDate.Date <= today && p.EndDate.Date >= today);

        var managers = _employeeManagerRepository.GetAll()
            .Where(m => m.StartDate.Date <= today && m.EndDate.Date >= today);

        var sponsors = _employeeSponsorRepository.GetAll()
            .Where(s => s.StartDate.Date <= today && s.EndDate.Date >= today);

        var accommodations = _employeeAccommodationRepository.GetAll()
            .Where(a => a.StartDate.Date <= today && a.EndDate.Date >= today);

        var accommodationServices = _accommodationServicesRepository.GetAll()
            .Where(a => a.StartDate.Date <= today && a.EndDate.Date >= today);

        // Legacy lines 781-789: the LATEST separation by submission date, revoked or not.
        var separations = _employeeSeparationRepository.GetAll();

        return query.Select(e => new SalaryBenefitsReportRowDto
        {
            // Legacy "OID" column. Exposed as Id because it is the row key.
            Id = e.Id,
            EmployeeCode = e.Code,
            // Legacy always wrote FullNameEn into this column; see the row DTO remarks.
            EmployeeName = e.FullNameEn,
            Status = e.Status.ToString(),
            JoiningDate = e.JoiningDate,

            JobTitleName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? null
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),

            // Legacy lines 822-838: the basic pay current today.
            EffectiveDate = basicPays
                .Where(p => p.EmployeeId == e.Id)
                .OrderBy(p => p.StartDate)
                .ThenBy(p => p.Id)
                .Select(p => (DateTime?)p.StartDate)
                .FirstOrDefault(),
            TotalSalary = basicPays
                .Where(p => p.EmployeeId == e.Id)
                .OrderBy(p => p.StartDate)
                .ThenBy(p => p.Id)
                .Select(p => (double?)p.Amount)
                .FirstOrDefault(),

            // Legacy line 745: Manager.CodeWithFullNameEn == Concat(Code, ' ', FullNameEn).
            ManagerName = managers
                .Where(m => m.EmployeeId == e.Id)
                .OrderBy(m => m.StartDate)
                .ThenBy(m => m.Id)
                .Select(m => m.Manager.Code + " " + m.Manager.FullNameEn)
                .FirstOrDefault(),

            // Legacy lines 726/845: the workflow group of today's org assignment, hard-coded NameEn.
            WorkflowGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkflowGroup == null ? null : o.WorkflowGroup.NameEn)
                .FirstOrDefault(),

            RecruitmentTypeName = e.RecruitmentType == null
                ? null
                : (useArabicNames ? e.RecruitmentType.NameAr : e.RecruitmentType.NameEn),

            // Legacy line 776: hard-coded NameEn.
            SponsorName = sponsors
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate)
                .ThenBy(s => s.Id)
                .Select(s => s.Sponsor.NameEn)
                .FirstOrDefault(),

            // Legacy lines 781-789/852-858: latest separation by submission date.
            SeparationType = separations
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .ThenByDescending(s => s.Id)
                .Select(s => s.SeparationType.ToString())
                .FirstOrDefault(),
            SubmissionDate = separations
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .ThenByDescending(s => s.Id)
                .Select(s => (DateTime?)s.SubmissionDate)
                .FirstOrDefault(),
            LastDayInService = separations
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .ThenByDescending(s => s.Id)
                .Select(s => (DateTime?)s.LastDayInService)
                .FirstOrDefault(),
            RevokeDate = separations
                .Where(s => s.EmployeeId == e.Id)
                .OrderByDescending(s => s.SubmissionDate)
                .ThenByDescending(s => s.Id)
                .Select(s => s.RevokeDate)
                .FirstOrDefault(),

            // Legacy lines 867-868: both hard-coded NameEn.
            AccommodationTypeName = accommodations
                .Where(a => a.EmployeeId == e.Id)
                .OrderBy(a => a.StartDate)
                .ThenBy(a => a.Id)
                .Select(a => a.AccommodationType.NameEn)
                .FirstOrDefault(),
            TransportationTypeName = accommodationServices
                .Where(a => a.EmployeeAccommodation.EmployeeId == e.Id)
                .OrderBy(a => a.StartDate)
                .ThenBy(a => a.Id)
                .Select(a => a.AccommodationServiceType.NameEn)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Phase two of materialization -- fills the columns that no translatable projection can produce:
    /// the per-wage-type pivot (legacy lines 549-556/832-835), <c>ServiceDuration</c> (lines 862-864)
    /// and <c>Age</c> (line 790). Returns the wage-type column list so the caller can hand it to the
    /// grid or the exporter. Called identically by the paged read and the export.
    /// </summary>
    private async Task<List<string>> PopulateDerivedValuesAsync(List<SalaryBenefitsReportRowDto> rows)
    {
        var today = Today;
        var useArabicNames = UseArabicNames;

        // Legacy lines 549-556: the distinct wage types appearing in any basic pay current today,
        // ordered by WageType.PayrollOrder. NOT limited to the rows on screen -- the column list is a
        // property of the master data, exactly as in legacy.
        var wageTypes = await _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate.Date <= today && d.EmployeeBasicPay.EndDate.Date >= today)
            .Select(d => new
            {
                d.WageTypeId,
                d.WageType.PayrollOrder,
                d.WageType.NameEn,
                d.WageType.NameAr
            })
            .Distinct()
            .OrderBy(d => d.PayrollOrder)
            .ToListAsync();

        // Legacy's own fallback (line 667): NameAr when Arabic AND set, otherwise NameEn.
        var wageTypeNameById = wageTypes.ToDictionary(
            w => w.WageTypeId,
            w => useArabicNames ? (w.NameAr ?? w.NameEn) : w.NameEn);

        var wageTypeNames = wageTypes
            .Select(w => wageTypeNameById[w.WageTypeId])
            .Distinct()
            .ToList();

        if (rows.Count == 0)
        {
            return wageTypeNames;
        }

        var employeeIds = rows.Select(r => r.Id).Distinct().ToList();

        var details = await _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate.Date <= today
                        && d.EmployeeBasicPay.EndDate.Date >= today
                        && employeeIds.Contains(d.EmployeeBasicPay.EmployeeId))
            .Select(d => new
            {
                d.EmployeeBasicPay.EmployeeId,
                d.WageTypeId,
                d.Amount
            })
            .ToListAsync();

        var detailsByEmployee = details
            .GroupBy(d => d.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Legacy line 862: measured to the last day in service of the latest NON-REVOKED separation.
        var separationEnds = await _employeeSeparationRepository.GetAll()
            .Where(s => s.RevokeDate == null && employeeIds.Contains(s.EmployeeId))
            .GroupBy(s => s.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, LastDayInService = g.Max(s => s.LastDayInService) })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.LastDayInService);

        var birthDates = await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.BirthDate })
            .ToDictionaryAsync(x => x.Id, x => x.BirthDate);

        var now = Clock.Now;

        foreach (var row in rows)
        {
            // Legacy skipped the whole wage block when the employee had no current basic pay
            // (the "if (empBasicSalary != null)" guard, line 830), leaving those cells DBNull.
            if (row.EffectiveDate.HasValue && detailsByEmployee.TryGetValue(row.Id, out var employeeDetails))
            {
                foreach (var wageType in wageTypes)
                {
                    var name = wageTypeNameById[wageType.WageTypeId];
                    // Legacy line 834: a wage type with no detail row for this employee is 0, not blank.
                    row.WageAmounts[name] = employeeDetails
                        .Where(d => d.WageTypeId == wageType.WageTypeId)
                        .Select(d => d.Amount)
                        .FirstOrDefault();
                }
            }

            var serviceEnd = separationEnds.TryGetValue(row.Id, out var lastDay) ? lastDay : now;
            row.ServiceDuration = ((serviceEnd - row.JoiningDate).TotalDays / 365.25)
                .ToString("0.0", CultureInfo.InvariantCulture);

            if (birthDates.TryGetValue(row.Id, out var birthDate))
            {
                // Legacy line 790, verbatim.
                row.Age = Math.Round(
                    (now.Year - birthDate.Year)
                    + ((now.Month - birthDate.Month) + (now.Day >= birthDate.Day ? 0 : -1)) / 12.0,
                    1);
            }
        }

        return wageTypeNames;
    }

    /// <summary>
    /// Exported columns in legacy column order, with the dynamic wage-type columns spliced in between
    /// EffectiveDate and TotalSalary exactly where legacy added them (lines 663-669). OID is omitted --
    /// a surrogate key.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<SalaryBenefitsReportRowDto>> GetExportColumns(
        List<string> wageTypeNames)
    {
        var columns = new List<GridReportColumn<SalaryBenefitsReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Status", r => r.Status),
            new("JoiningDate", r => r.JoiningDate),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("EffectiveDate", r => r.EffectiveDate)
        };

        foreach (var wageTypeName in wageTypeNames)
        {
            var name = wageTypeName;
            // FromLiteralHeader, not a localization key: these headers are master data (wage type names)
            // resolved at runtime, exactly as legacy named its dynamic columns (line 667).
            columns.Add(GridReportColumn<SalaryBenefitsReportRowDto>.FromLiteralHeader(
                name,
                r => r.WageAmounts.TryGetValue(name, out var amount) ? amount : (double?)null));
        }

        columns.AddRange(new[]
        {
            new GridReportColumn<SalaryBenefitsReportRowDto>("TotalSalary", r => r.TotalSalary),
            new GridReportColumn<SalaryBenefitsReportRowDto>("Manager", r => r.ManagerName),
            new GridReportColumn<SalaryBenefitsReportRowDto>("WorkflowGroup", r => r.WorkflowGroupName),
            new GridReportColumn<SalaryBenefitsReportRowDto>("RecruitmentType", r => r.RecruitmentTypeName),
            new GridReportColumn<SalaryBenefitsReportRowDto>("Sponsor", r => r.SponsorName),
            new GridReportColumn<SalaryBenefitsReportRowDto>("SeparationType", r => r.SeparationType),
            new GridReportColumn<SalaryBenefitsReportRowDto>("SubmissionDate", r => r.SubmissionDate),
            new GridReportColumn<SalaryBenefitsReportRowDto>("LastDayInService", r => r.LastDayInService),
            new GridReportColumn<SalaryBenefitsReportRowDto>("RevokeDate", r => r.RevokeDate),
            new GridReportColumn<SalaryBenefitsReportRowDto>("ServiceDuration", r => r.ServiceDuration),
            new GridReportColumn<SalaryBenefitsReportRowDto>("Age", r => r.Age),
            new GridReportColumn<SalaryBenefitsReportRowDto>("Accommodation", r => r.AccommodationTypeName),
            new GridReportColumn<SalaryBenefitsReportRowDto>("TransportationType", r => r.TransportationTypeName)
        });

        return columns;
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<SalaryBenefitsReportRowDto> rows)
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
