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
/// Employee Document Movement analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeDocMovement.ascx(.cs)</c>, whose
/// entire data contract is <c>Table1()</c> (lines 492-678).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button,
/// and the dead <c>Table1_Old()</c> (lines 679-847).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeDocMovement)]
public class EmployeeDocMovementReportAppService : GridReportAppServiceBase, IEmployeeDocMovementReportAppService
{
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IRepository<EmployeeDocumentMvmt> _employeeDocumentMvmtRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeDocMovementReportAppService(
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IRepository<EmployeeDocumentMvmt> employeeDocumentMvmtRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeDocumentRepository = employeeDocumentRepository;
        _employeeDocumentMvmtRepository = employeeDocumentMvmtRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeDocMovement";

    protected override string ScreenCaption => "Employee Document Movement";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDocMovementReportRowDto>> GetEmployeeDocMovementReport(
        GetEmployeeDocMovementReportInput input)
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

        return new PagedResultDto<EmployeeDocMovementReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeDocMovementReportToExcel(GetEmployeeDocMovementReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeDocMovement.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeDocMovement.ascx.cs:492-678).
    /// </summary>
    private async Task<IQueryable<EmployeeDocMovementReportRowDto>> BuildQueryAsync(
        GetEmployeeDocMovementReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var mvmtStatusFilter = input.DocumentMvmtStatus;

        var query = _employeeDocumentRepository.GetAll()
            // Legacy line 566: !(IssueDate > dtTo || ExpiryDate < dtFrom), i.e. an overlapping window.
            .Where(d => !(d.IssueDate.Date > toDate || d.ExpiryDate.Date < fromDate))
            .WhereIf(employeeStatus.HasValue, d => d.Employee.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId));

        // Legacy line 165 equivalent (EmployeeDocumentsAppService.ToDtoAsync): OUT when an open (not yet
        // actually-returned) movement exists, else IN. Reused verbatim so the filter and the exported
        // column cannot drift from the Employee Documents screen's own computed status.
        if (mvmtStatusFilter.HasValue)
        {
            var wantOpen = mvmtStatusFilter.Value == DocumentMvmtStatus.OUT;
            query = query.Where(d =>
                _employeeDocumentMvmtRepository.GetAll()
                    .Any(m => m.EmployeeDocumentId == d.Id && m.ActualReturnDate == default(DateTime)) == wantOpen);
        }

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= Today && o.EndDate.Date >= Today);

        // Legacy line 619: the first movement with ActualReturnDate != null (closed). Ordered by Id here
        // for a deterministic pick that the legacy in-memory evaluation did not guarantee.
        var closedMovements = _employeeDocumentMvmtRepository.GetAll()
            .Where(m => m.ActualReturnDate != default(DateTime));

        return query.Select(d => new EmployeeDocMovementReportRowDto
        {
            Id = d.Id,
            EmployeeCode = d.Employee.Code,
            EmployeeName = useArabicNames ? d.Employee.FullNameAr : d.Employee.FullNameEn,
            JoiningDate = d.Employee.JoiningDate,
            DocumentTypeName = useArabicNames ? d.DocumentType.NameAr : d.DocumentType.NameEn,
            DocumentMvmtStatus = _employeeDocumentMvmtRepository.GetAll()
                .Any(m => m.EmployeeDocumentId == d.Id && m.ActualReturnDate == default(DateTime))
                    ? PersonnelAdministration.DocumentMvmtStatus.OUT
                    : PersonnelAdministration.DocumentMvmtStatus.IN,
            WithdrawDate = closedMovements
                .Where(m => m.EmployeeDocumentId == d.Id)
                .OrderBy(m => m.Id)
                .Select(m => (DateTime?)m.WithdrawDate)
                .FirstOrDefault(),
            ReturnDate = closedMovements
                .Where(m => m.EmployeeDocumentId == d.Id)
                .OrderBy(m => m.Id)
                .Select(m => (DateTime?)m.ReturnDate)
                .FirstOrDefault(),
            ActualReturnDate = closedMovements
                .Where(m => m.EmployeeDocumentId == d.Id)
                .OrderBy(m => m.Id)
                .Select(m => (DateTime?)m.ActualReturnDate)
                .FirstOrDefault(),
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? string.Empty : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            CountryName = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null ? null : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault()
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeDocMovementReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeDocMovementReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("JoiningDate", r => r.JoiningDate),
            new("DocumentType", r => r.DocumentTypeName),
            new("DocumentMvmtStatus", r => r.DocumentMvmtStatus.ToString()),
            new("WithdrawDate", r => r.WithdrawDate),
            new("ReturnDate", r => r.ReturnDate),
            new("ActualReturnDate", r => r.ActualReturnDate),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocationName),
            new("Country", r => r.CountryName)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeDocMovementReportRowDto> rows)
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
