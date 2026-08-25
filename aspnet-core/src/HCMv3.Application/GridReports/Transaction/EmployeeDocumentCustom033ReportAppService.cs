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
/// Employee Document (Custom033) analysis grid report -- customer-specific variant, port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeDocument_Custom033.ascx(.cs)</c>.
/// See <see cref="EmployeeDocumentCustom033ReportRowDto"/> remarks for the exact diff against the base
/// <see cref="EmployeeDocumentReportAppService"/> (adds CivilId, renames Sponsor -&gt; SponsorName).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeDocumentCustom033)]
public class EmployeeDocumentCustom033ReportAppService : GridReportAppServiceBase, IEmployeeDocumentCustom033ReportAppService
{
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeDocumentCustom033ReportAppService(
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

    protected override string LegacyScreenName => "uscRptEmployeeDocument_Custom033";

    protected override string ScreenCaption => "Employee Document (Custom033)";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDocumentCustom033ReportRowDto>> GetEmployeeDocumentCustom033Report(
        GetEmployeeDocumentCustom033ReportInput input)
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

        return new PagedResultDto<EmployeeDocumentCustom033ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeDocumentCustom033ReportToExcel(GetEmployeeDocumentCustom033ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeDocumentCustom033.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    private async Task<IQueryable<EmployeeDocumentCustom033ReportRowDto>> BuildQueryAsync(
        GetEmployeeDocumentCustom033ReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var documentTypeCode = input.DocumentTypeCode;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var query = _employeeDocumentRepository.GetAll()
            .Where(d => d.IssueDate.Date <= toDate && d.ExpiryDate.Date >= fromDate)
            .WhereIf(!documentTypeCode.IsNullOrWhiteSpace(), d => d.DocumentType.Code == documentTypeCode)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId));

        return query.Select(d => new EmployeeDocumentCustom033ReportRowDto
        {
            Id = d.Id,
            EmployeeCode = d.Employee.Code,
            EmployeeName = useArabicNames ? d.Employee.FullNameAr : d.Employee.FullNameEn,
            // Custom033-only column (legacy line 602: rec.Employee.CivilID).
            CivilId = d.Employee.CivilID,
            EmployeeStatus = d.Employee.Status,
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
            SponsorName = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == d.EmployeeId && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.Sponsor == null ? string.Empty : (useArabicNames ? s.Sponsor.NameAr : s.Sponsor.NameEn))
                .FirstOrDefault(),
            DocumentTypeName = d.DocumentType == null
                ? string.Empty
                : (useArabicNames ? d.DocumentType.NameAr : d.DocumentType.NameEn),
            IssueDate = d.IssueDate,
            ExpiryDate = d.ExpiryDate,
            IsActive = d.IssueDate.Date <= today && d.ExpiryDate.Date >= today,
            DocumentNum = d.DocumentNum,
            Description = d.Description,
            DaysLeft = (d.ExpiryDate.Date - today).Days
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeDocumentCustom033ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeDocumentCustom033ReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("CivilId", r => r.CivilId),
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
            new("SponsorName", r => r.SponsorName),
            new("DocumentType", r => r.DocumentTypeName),
            new("IssueDate", r => r.IssueDate),
            new("ExpiryDate", r => r.ExpiryDate),
            new("IsActive", r => r.IsActive),
            new("DocumentNum", r => r.DocumentNum),
            new("Description", r => r.Description),
            new("DaysLeft", r => r.DaysLeft)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeDocumentCustom033ReportRowDto> rows)
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
