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
/// Employee Document analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeDocument.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 495-677). See <see cref="EmployeeDocumentReportRowDto"/> remarks for the
/// IsActive/DaysLeft simplification.
///
/// NOT PORTED (DevExpress/WebForms UI plumbing, no data meaning): the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 686+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeDocument)]
public class EmployeeDocumentReportAppService : GridReportAppServiceBase, IEmployeeDocumentReportAppService
{
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeDocumentReportAppService(
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeDocumentRepository = employeeDocumentRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeDocument";

    protected override string ScreenCaption => "Employee Document";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDocumentReportRowDto>> GetEmployeeDocumentReport(
        GetEmployeeDocumentReportInput input)
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

        return new PagedResultDto<EmployeeDocumentReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeDocumentReportToExcel(GetEmployeeDocumentReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeDocument.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeDocument.ascx.cs:569-636).
    /// </summary>
    private async Task<IQueryable<EmployeeDocumentReportRowDto>> BuildQueryAsync(GetEmployeeDocumentReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var documentTypeCode = input.DocumentTypeCode;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var query = _employeeDocumentRepository.GetAll()
            // Legacy lines 570-571: !(IssueDate > dtTo || ExpiryDate < dtFrom), an overlap test.
            .Where(d => d.IssueDate.Date <= toDate && d.ExpiryDate.Date >= fromDate)
            // Legacy lines 576-579: r.DocumentType.Code == _code.
            .WhereIf(!documentTypeCode.IsNullOrWhiteSpace(), d => d.DocumentType.Code == documentTypeCode)
            // Legacy lines 580-584: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.Employee.Code))
            // Legacy line 586 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId));

        return query.Select(d => new EmployeeDocumentReportRowDto
        {
            Id = d.Id,
            EmployeeCode = d.Employee.Code,
            EmployeeName = useArabicNames ? d.Employee.FullNameAr : d.Employee.FullNameEn,
            EmployeeStatus = d.Employee.Status,
            // Legacy lines 588-617: JobTitle/OrgLevel1..7/CostCenter/PayrollArea/Country from the org
            // assignment current TODAY (not the document dates).
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
            // Legacy lines 591-593/619-623: EmployeeSponsor current TODAY.
            Sponsor = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == d.EmployeeId && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.Sponsor == null ? string.Empty : (useArabicNames ? s.Sponsor.NameAr : s.Sponsor.NameEn))
                .FirstOrDefault(),
            DocumentTypeName = d.DocumentType == null
                ? string.Empty
                : (useArabicNames ? d.DocumentType.NameAr : d.DocumentType.NameEn),
            IssueDate = d.IssueDate,
            ExpiryDate = d.ExpiryDate,
            // Simplified from legacy's fuller computed rule -- see DTO remarks.
            IsActive = d.IssueDate.Date <= today && d.ExpiryDate.Date >= today,
            DocumentNum = d.DocumentNum,
            Description = d.Description,
            DaysLeft = (d.ExpiryDate.Date - today).Days
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeDocumentReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeDocumentReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("EmployeeStatus", r => r.EmployeeStatus),
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
            new("Country", r => r.Country),
            new("Sponsor", r => r.Sponsor),
            new("DocumentType", r => r.DocumentTypeName),
            new("IssueDate", r => r.IssueDate),
            new("ExpiryDate", r => r.ExpiryDate),
            new("IsActive", r => r.IsActive),
            new("DocumentNum", r => r.DocumentNum),
            new("Description", r => r.Description),
            new("DaysLeft", r => r.DaysLeft)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeDocumentReportRowDto> rows)
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
