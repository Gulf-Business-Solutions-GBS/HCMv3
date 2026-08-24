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
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Status analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeStatus.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 484-676).
///
/// The report's point is the DERIVED lifecycle label (Separation / Probationary / New Join), not
/// <c>Employee.Status</c>; see <see cref="EmployeeStatusReportRowDto.StatusLabel"/>.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (its <c>GetTable()</c> keeps the Table2/Table3 cases commented out, lines 436-443); the
/// ColumnChooser JSON persistence; the callback-panel state machine; and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeStatus)]
public class EmployeeStatusReportAppService : GridReportAppServiceBase, IEmployeeStatusReportAppService
{
    private const string BasicPayWageTypeCode = "1000";

    /// <summary>Legacy <c>uscRptEmployeeStatus.ascx.cs:51</c>. Not localized -- see the DTO remarks.</summary>
    private const string StatusSeparation = "Separation";

    /// <summary>Legacy <c>uscRptEmployeeStatus.ascx.cs:50</c>.</summary>
    private const string StatusProbationary = "Probationary";

    /// <summary>Legacy <c>uscRptEmployeeStatus.ascx.cs:49</c>.</summary>
    private const string StatusNewJoin = "New Join";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeStatusReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeStatus";

    protected override string ScreenCaption => "Employee Status";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeStatusReportRowDto>> GetEmployeeStatusReport(
        GetEmployeeStatusReportInput input)
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

        return new PagedResultDto<EmployeeStatusReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeStatusReportToExcel(GetEmployeeStatusReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeStatus.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeStatus.ascx.cs:484-676).
    /// </summary>
    private async Task<IQueryable<EmployeeStatusReportRowDto>> BuildQueryAsync(
        GetEmployeeStatusReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Every date-effective lookup here is "as of now", exactly as legacy had it.
        var today = Today;
        var yearStart = new DateTime(today.Year, 1, 1);
        var yearEnd = new DateTime(today.Year, 12, 31);

        var query = _employeeRepository.GetAll()
            // DEVIATION (see the input DTO): legacy has these two filters commented out at lines 490-501,
            // so its filter boxes did nothing. They are wired up here.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy line 503 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        // Legacy lines 569-583: the org assignment current today (legacy reached it through the
        // Employee.Current* computed properties, which meant the same thing).
        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        // Legacy lines 642-644: the basic pay record current today.
        var basicPaysToday = _employeeBasicPayRepository.GetAll()
            .Where(p => p.StartDate <= today && p.EndDate >= today);

        var payDetailsToday = _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= today && d.EmployeeBasicPay.EndDate >= today);

        return query.Select(e => new EmployeeStatusReportRowDto
        {
            // Legacy "OID" column -- the row key, not a data column.
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn,
            IsActive = e.Status == EmployeeStatus.Active,
            BirthDate = e.BirthDate,
            JoiningDate = e.JoiningDate,
            // Legacy lines 620-640: a first-match ladder. The FnF branch deliberately yields an EMPTY
            // label because legacy's assignment inside it is commented out (lines 625-628).
            StatusLabel =
                _employeeFnFPaymentRepository.GetAll().Any(f => f.EmployeeId == e.Id)
                    ? string.Empty
                    : _employeeSeparationRepository.GetAll().Any(s => s.EmployeeId == e.Id)
                        ? StatusSeparation
                        : e.ProbationEndDate >= today
                            ? StatusProbationary
                            : e.JoiningDate >= yearStart && e.JoiningDate <= yearEnd
                                ? StatusNewJoin
                                : string.Empty,
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
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            // Legacy line 646: only wage type 1000.
            BasicPay = payDetailsToday
                .Where(d => d.EmployeeBasicPay.EmployeeId == e.Id && d.WageType.Code == BasicPayWageTypeCode)
                .Sum(d => d.Amount),
            // Legacy line 647: every other wage type flagged IncludeInBasicPay.
            Allowance = payDetailsToday
                .Where(d => d.EmployeeBasicPay.EmployeeId == e.Id
                            && d.WageType.Code != BasicPayWageTypeCode
                            && d.WageType.IncludeInBasicPay)
                .Sum(d => d.Amount),
            // Legacy line 648: the basic-pay HEADER amount, not the detail sum.
            TotalSalary = basicPaysToday
                .Where(p => p.EmployeeId == e.Id)
                .OrderBy(p => p.StartDate).ThenBy(p => p.Id)
                .Select(p => p.Amount)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted; OrgLevel1..7 is
    /// the single OrganizationUnit column; the two permanently-blank legacy columns are absent (see the
    /// row DTO remarks).
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeStatusReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeStatusReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("IsActive", r => r.IsActive),
            new("Status", r => r.StatusLabel),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("BirthDate", r => r.BirthDate),
            new("JoiningDate", r => r.JoiningDate),
            new("BasicPay", r => r.BasicPay),
            new("Allowance", r => r.Allowance),
            new("TotalSalary", r => r.TotalSalary),
            new("WorkLocation", r => r.WorkLocationName)
        };
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeStatusReportRowDto> rows)
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
