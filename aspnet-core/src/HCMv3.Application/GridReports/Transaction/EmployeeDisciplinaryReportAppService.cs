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
/// Employee Disciplinary analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeDisciplinary.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 495-623).
///
/// LEGACY DISPLAY QUIRK, NOT REPRODUCED: legacy line 583 assigned the raw <c>DisciplinaryDeductionType</c>
/// FK object (not its Name) to the DataTable cell, relying on the XPO business object's default
/// <c>ToString()</c> -- meaningless once ported to EF Core entities (no XPO <c>ToString()</c> override
/// exists here). <see cref="EmployeeDisciplinaryReportRowDto.DisciplinaryDeductionTypeName"/> resolves the
/// proper NameEn/NameAr instead, consistent with every other lookup column in this report.
///
/// NOT PORTED (DevExpress/WebForms UI plumbing, no data meaning): the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 624+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeDisciplinary)]
public class EmployeeDisciplinaryReportAppService : GridReportAppServiceBase, IEmployeeDisciplinaryReportAppService
{
    private readonly IRepository<EmployeeDisciplinary> _employeeDisciplinaryRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeDisciplinaryReportAppService(
        IRepository<EmployeeDisciplinary> employeeDisciplinaryRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeDisciplinaryRepository = employeeDisciplinaryRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeDisciplinary";

    protected override string ScreenCaption => "Employee Disciplinary";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDisciplinaryReportRowDto>> GetEmployeeDisciplinaryReport(
        GetEmployeeDisciplinaryReportInput input)
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

        return new PagedResultDto<EmployeeDisciplinaryReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeDisciplinaryReportToExcel(GetEmployeeDisciplinaryReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeDisciplinary.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    private async Task<IQueryable<EmployeeDisciplinaryReportRowDto>> BuildQueryAsync(
        GetEmployeeDisciplinaryReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var disciplinaryTypeCode = input.DisciplinaryTypeCode;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var query = _employeeDisciplinaryRepository.GetAll()
            // Legacy lines 538-539: plain DisciplinaryDate range.
            .Where(d => d.DisciplinaryDate >= fromDate && d.DisciplinaryDate <= toDate)
            // Legacy lines 543-546: r.DisciplinaryType.Code == _code.
            .WhereIf(!disciplinaryTypeCode.IsNullOrWhiteSpace(), d => d.DisciplinaryType != null && d.DisciplinaryType.Code == disciplinaryTypeCode)
            // Legacy lines 547-551: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.Employee.Code))
            // Legacy line 553 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId));

        return query.Select(d => new EmployeeDisciplinaryReportRowDto
        {
            Id = d.Id,
            EmployeeCode = d.Employee.Code,
            EmployeeName = useArabicNames ? d.Employee.FullNameAr : d.Employee.FullNameEn,
            // Legacy lines 555-579: JobTitle/OrgLevel1..7/CostCenter/PayrollArea/Country from the org
            // assignment current TODAY (not the disciplinary date).
            JobTitle = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == d.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == d.EmployeeId && o.StartDate <= today && o.EndDate >= today && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            CostCenter = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == d.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null ? string.Empty : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            PayrollArea = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == d.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null ? string.Empty : o.PayrollArea.Name)
                .FirstOrDefault(),
            Country = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == d.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null || o.PayrollArea.Country == null
                    ? string.Empty
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault(),
            DisciplinaryDate = d.DisciplinaryDate,
            DisciplinaryTypeName = d.DisciplinaryType == null
                ? string.Empty
                : (useArabicNames ? d.DisciplinaryType.NameAr : d.DisciplinaryType.NameEn),
            DisciplinaryDeductionTypeName = d.DisciplinaryDeductionType == null
                ? string.Empty
                : (useArabicNames ? d.DisciplinaryDeductionType.NameAr : d.DisciplinaryDeductionType.NameEn),
            Comments = d.Comments ?? string.Empty
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeDisciplinaryReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeDisciplinaryReportRowDto>>
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
            new("CostCenter", r => r.CostCenter),
            new("PayrollArea", r => r.PayrollArea),
            new("DisciplinaryDate", r => r.DisciplinaryDate),
            new("DisciplinaryType", r => r.DisciplinaryTypeName),
            new("DisciplinaryDeductionType", r => r.DisciplinaryDeductionTypeName),
            new("Comments", r => r.Comments),
            new("Country", r => r.Country)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeDisciplinaryReportRowDto> rows)
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
