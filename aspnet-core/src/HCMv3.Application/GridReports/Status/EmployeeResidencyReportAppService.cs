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
/// Employee Residency analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeResidency.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 493-793).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button,
/// and the dead <c>Table1_Old()</c> (lines 794+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeResidency)]
public class EmployeeResidencyReportAppService : GridReportAppServiceBase, IEmployeeResidencyReportAppService
{
    private static readonly string[] CivilIdDocumentTypeNames = { "CID", "Civil Id" };
    private const string PassportDocumentTypeNameEn = "Passport";
    private const string ResidenceExtensionDocumentTypeNameEn = "Residence Extension";
    private const string RenewedResidenceInfoDocumentTypeNameEn = "Renewed Residence Information";

    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeResidencyReportAppService(
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

    protected override string LegacyScreenName => "uscRptEmployeeResidency";

    protected override string ScreenCaption => "Employee Residency";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeResidencyReportRowDto>> GetEmployeeResidencyReport(
        GetEmployeeResidencyReportInput input)
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

        return new PagedResultDto<EmployeeResidencyReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeResidencyReportToExcel(GetEmployeeResidencyReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeResidency.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeResidency.ascx.cs:493-793). ONE ROW PER CIVIL-ID
    /// DOCUMENT, matching legacy's own row shape -- see the row DTO remarks.
    /// </summary>
    private async Task<IQueryable<EmployeeResidencyReportRowDto>> BuildQueryAsync(GetEmployeeResidencyReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var residencyType = input.ResidencyType;
        var today = Today;

        var query = _employeeDocumentRepository.GetAll()
            .Where(d => d.DocumentType != null
                        && CivilIdDocumentTypeNames.Contains(d.DocumentType.NameEn)
                        && !(d.IssueDate.Date > toDate || d.ExpiryDate.Date < fromDate))
            .WhereIf(employeeStatus.HasValue, d => d.Employee.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        // Legacy lines 643-656/764: the sponsor record current TODAY (not the civil-id document's dates).
        var sponsorsToday = _employeeSponsorRepository.GetAll()
            .Where(s => s.StartDate.Date <= today && s.EndDate.Date >= today);

        if (residencyType.HasValue)
        {
            query = query.Where(d => sponsorsToday
                .Where(s => s.EmployeeId == d.EmployeeId)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (ResidencyType?)s.ResidencyType)
                .FirstOrDefault() == residencyType.Value);
        }

        // Legacy lines 694-716: among the employee's Passport documents currently valid (no expiry, or
        // Issue<=today<=Expiry), the one with the latest IssueDate.
        var validPassports = _employeeDocumentRepository.GetAll()
            .Where(p => p.DocumentType != null
                        && p.DocumentType.NameEn == PassportDocumentTypeNameEn
                        && (p.ExpiryDate == default(DateTime) || (p.IssueDate.Date <= today && p.ExpiryDate.Date >= today)));

        var residenceExtensions = _employeeDocumentRepository.GetAll()
            .Where(r => r.DocumentType != null && r.DocumentType.NameEn == ResidenceExtensionDocumentTypeNameEn);

        var renewedResidenceInfos = _employeeDocumentRepository.GetAll()
            .Where(r => r.DocumentType != null && r.DocumentType.NameEn == RenewedResidenceInfoDocumentTypeNameEn);

        return query.Select(d => new EmployeeResidencyReportRowDto
        {
            Id = d.Id,
            EmployeeCode = d.Employee.Code,
            EmployeeName = useArabicNames ? d.Employee.FullNameAr : d.Employee.FullNameEn,
            JoiningDate = d.Employee.JoiningDate,
            CivilId = d.Employee.CivilID,
            CivilIdIssueDate = d.IssueDate,
            CivilIdExpiryDate = d.ExpiryDate,
            PassportNo = validPassports
                .Where(p => p.EmployeeId == d.EmployeeId)
                .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                .Select(p => p.DocumentNum)
                .FirstOrDefault(),
            PassportIssueDate = validPassports
                .Where(p => p.EmployeeId == d.EmployeeId)
                .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                .Select(p => (DateTime?)p.IssueDate)
                .FirstOrDefault(),
            PassportExpiryDate = validPassports
                .Where(p => p.EmployeeId == d.EmployeeId)
                .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                .Select(p => (DateTime?)p.ExpiryDate)
                .FirstOrDefault(),
            ResidenceExtensionIssueDate = residenceExtensions
                .Where(r => r.EmployeeId == d.EmployeeId)
                .OrderByDescending(r => r.ExpiryDate).ThenByDescending(r => r.Id)
                .Select(r => (DateTime?)r.IssueDate)
                .FirstOrDefault(),
            ResidenceExtensionExpiryDate = residenceExtensions
                .Where(r => r.EmployeeId == d.EmployeeId)
                .OrderByDescending(r => r.ExpiryDate).ThenByDescending(r => r.Id)
                .Select(r => (DateTime?)r.ExpiryDate)
                .FirstOrDefault(),
            RenewedResidenceInfoIssueDate = renewedResidenceInfos
                .Where(r => r.EmployeeId == d.EmployeeId)
                .OrderByDescending(r => r.ExpiryDate).ThenByDescending(r => r.Id)
                .Select(r => (DateTime?)r.IssueDate)
                .FirstOrDefault(),
            RenewedResidenceInfoExpiryDate = renewedResidenceInfos
                .Where(r => r.EmployeeId == d.EmployeeId)
                .OrderByDescending(r => r.ExpiryDate).ThenByDescending(r => r.Id)
                .Select(r => (DateTime?)r.ExpiryDate)
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? string.Empty : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            ResidencyType = sponsorsToday
                .Where(s => s.EmployeeId == d.EmployeeId)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (ResidencyType?)s.ResidencyType)
                .FirstOrDefault(),
            SalaryInIznamal = sponsorsToday
                .Where(s => s.EmployeeId == d.EmployeeId)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (double?)s.ContractSalary)
                .FirstOrDefault(),
            GovtJobTitleName = sponsorsToday
                .Where(s => s.EmployeeId == d.EmployeeId)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.GovtJobTitle == null ? string.Empty : s.GovtJobTitle.NameEn)
                .FirstOrDefault(),
            GovtJobTitleNameAr = sponsorsToday
                .Where(s => s.EmployeeId == d.EmployeeId)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.GovtJobTitle == null ? string.Empty : s.GovtJobTitle.NameAr)
                .FirstOrDefault(),
            StartDate = sponsorsToday
                .Where(s => s.EmployeeId == d.EmployeeId)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (DateTime?)s.StartDate)
                .FirstOrDefault(),
            EndDate = sponsorsToday
                .Where(s => s.EmployeeId == d.EmployeeId)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (DateTime?)s.EndDate)
                .FirstOrDefault(),
            CountryName = currentOrgAssignments
                .Where(o => o.EmployeeId == d.EmployeeId && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null ? null : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault()
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeResidencyReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeResidencyReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("JoiningDate", r => r.JoiningDate),
            new("CivilId", r => r.CivilId),
            new("CivilIdIssueDate", r => r.CivilIdIssueDate),
            new("CivilIdExpiryDate", r => r.CivilIdExpiryDate),
            new("PassportNo", r => r.PassportNo),
            new("PassportIssueDate", r => r.PassportIssueDate),
            new("PassportExpiryDate", r => r.PassportExpiryDate),
            new("ResidenceExtensionIssueDate", r => r.ResidenceExtensionIssueDate),
            new("ResidenceExtensionExpiryDate", r => r.ResidenceExtensionExpiryDate),
            new("RenewedResidenceInfoIssueDate", r => r.RenewedResidenceInfoIssueDate),
            new("RenewedResidenceInfoExpiryDate", r => r.RenewedResidenceInfoExpiryDate),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocationName),
            new("ResidencyType", r => r.ResidencyType?.ToString()),
            new("SalaryInIznamal", r => r.SalaryInIznamal),
            new("GovtJobTitle", r => r.GovtJobTitleName),
            new("GovtJobTitleAr", r => r.GovtJobTitleNameAr),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("Country", r => r.CountryName)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeResidencyReportRowDto> rows)
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
