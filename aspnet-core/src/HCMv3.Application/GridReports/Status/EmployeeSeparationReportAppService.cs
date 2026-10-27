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
/// Employee Separation analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeSeparation.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 485-789). See <see cref="Dto.EmployeeSeparationReportRowDto"/>
/// for the two columns that could not be ported (entity gap from an earlier wave).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeSeparation)]
public class EmployeeSeparationReportAppService : GridReportAppServiceBase, IEmployeeSeparationReportAppService
{
    private const string PublicHolidayAbsenceTypeCode = "9";
    private static readonly string[] CivilIdDocumentTypeNames = { "CID", "Civil Id" };

    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _employeeAbsenceQuotaRepository;
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeSeparationReportAppService(
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeAbsenceQuota> employeeAbsenceQuotaRepository,
        IRepository<EmployeeDocument> employeeDocumentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeAbsenceQuotaRepository = employeeAbsenceQuotaRepository;
        _employeeDocumentRepository = employeeDocumentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeSeparation";

    protected override string ScreenCaption => "Employee Separation";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSeparationReportRowDto>> GetEmployeeSeparationReport(
        GetEmployeeSeparationReportInput input)
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

        return new PagedResultDto<EmployeeSeparationReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeSeparationReportToExcel(GetEmployeeSeparationReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeSeparation.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeSeparation.ascx.cs:485-789).
    /// </summary>
    private async Task<IQueryable<EmployeeSeparationReportRowDto>> BuildQueryAsync(GetEmployeeSeparationReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var separationType = input.SeparationType;
        var isRevoked = input.IsRevoked;
        var today = Today;

        var query = _employeeSeparationRepository.GetAll()
            // Legacy lines 591-593.
            .Where(s => s.LastDayInService >= fromDate && s.LastDayInService <= toDate)
            .WhereIf(separationType.HasValue, s => s.SeparationType == separationType.Value)
            .WhereIf(employeeCodes.Count > 0, s => employeeCodes.Contains(s.Employee.Code))
            // Legacy lines 609-621: "Active" = RevokeDate == null, "Inactive" = RevokeDate != null.
            .WhereIf(isRevoked == true, s => s.RevokeDate != null)
            .WhereIf(isRevoked == false, s => s.RevokeDate == null)
            .WhereIf(rlsEmployeeIds.Count > 0, s => rlsEmployeeIds.Contains(s.EmployeeId));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        var absenceQuotas = _employeeAbsenceQuotaRepository.GetAll()
            .Where(q => q.AbsenceType.Code == PublicHolidayAbsenceTypeCode);

        var civilIdDocuments = _employeeDocumentRepository.GetAll()
            .Where(d => d.DocumentType != null && CivilIdDocumentTypeNames.Contains(d.DocumentType.NameEn));

        return query.Select(s => new EmployeeSeparationReportRowDto
        {
            Id = s.Id,
            EmployeeCode = s.Employee.Code,
            EmployeeName = useArabicNames ? s.Employee.FullNameAr : s.Employee.FullNameEn,
            CivilId = s.Employee.CivilID,
            // Legacy lines 645-651/701/740: most recent basic pay by StartDate, no date-window.
            Salary = _employeeBasicPayRepository.GetAll()
                .Where(p => p.EmployeeId == s.EmployeeId)
                .OrderByDescending(p => p.StartDate).ThenByDescending(p => p.Id)
                .Select(p => (double?)p.Amount)
                .FirstOrDefault() ?? 0,
            JoiningDate = s.Employee.JoiningDate,
            SeparationDate = s.SubmissionDate,
            NoticePeriodDays = s.NoticePeriodDays,
            LastDayInService = s.LastDayInService,
            NoticePeriodEndQuirk = s.SubmissionDate.AddDays(89),
            PlannedLastDayInService = s.PlannedLastDayInService,
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == s.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == s.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == s.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? string.Empty : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            SeparationType = s.SeparationType,
            SeparationReasonName = s.SeparationReason == null ? string.Empty : (useArabicNames ? s.SeparationReason.NameAr : s.SeparationReason.NameEn),
            // Legacy lines 707-709: the sponsor window covering the SubmissionDate.
            ResidencyType = _employeeSponsorRepository.GetAll()
                .Where(sp => sp.EmployeeId == s.EmployeeId && sp.StartDate <= s.SubmissionDate && sp.EndDate >= s.SubmissionDate)
                .OrderBy(sp => sp.StartDate).ThenBy(sp => sp.Id)
                .Select(sp => (ResidencyType?)sp.ResidencyType)
                .FirstOrDefault(),
            ResidencyExpiry = civilIdDocuments
                .Where(d => d.EmployeeId == s.EmployeeId)
                .OrderByDescending(d => d.ExpiryDate).ThenByDescending(d => d.Id)
                .Select(d => (DateTime?)d.ExpiryDate)
                .FirstOrDefault(),
            PHBalance = absenceQuotas
                .Where(q => q.EmployeeId == s.EmployeeId)
                .OrderBy(q => q.Id)
                .Select(q => (double?)(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted + q.QuotaForfeited)))
                .FirstOrDefault() ?? 0,
            PayStatus = absenceQuotas
                .Where(q => q.EmployeeId == s.EmployeeId)
                .OrderBy(q => q.Id)
                .Select(q => (double?)q.QuotaUsed)
                .FirstOrDefault() ?? 0,
            CountryName = currentOrgAssignments
                .Where(o => o.EmployeeId == s.EmployeeId && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null ? null : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault(),
            RevokeDate = s.RevokeDate,
            CostCenterName = currentOrgAssignments
                .Where(o => o.EmployeeId == s.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null ? string.Empty : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            CostCenterGroupName = currentOrgAssignments
                .Where(o => o.EmployeeId == s.EmployeeId && o.CostCenter != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter.CostCenterGroup == null ? string.Empty : (useArabicNames ? o.CostCenter.CostCenterGroup.NameAr : o.CostCenter.CostCenterGroup.NameEn))
                .FirstOrDefault()
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeSeparationReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeSeparationReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("CivilId", r => r.CivilId),
            new("Salary", r => r.Salary),
            new("JoiningDate", r => r.JoiningDate),
            new("SeparationDate", r => r.SeparationDate),
            new("NoticePeriodDays", r => r.NoticePeriodDays),
            new("ActualLastWorkingDay", r => r.LastDayInService),
            new("NoticePeriodEnd", r => r.NoticePeriodEndQuirk),
            new("PlannedLastDayInService", r => r.PlannedLastDayInService),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocationName),
            new("SeparationType", r => r.SeparationType.ToString()),
            new("SeparationReason", r => r.SeparationReasonName),
            new("ResidencyType", r => r.ResidencyType?.ToString()),
            new("ResidencyExpiry", r => r.ResidencyExpiry),
            new("PHBalance", r => r.PHBalance),
            new("PayStatus", r => r.PayStatus),
            new("Country", r => r.CountryName),
            new("RevokeDate", r => r.RevokeDate),
            new("CostCenter", r => r.CostCenterName),
            new("CostCenterGroup", r => r.CostCenterGroupName)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeSeparationReportRowDto> rows)
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
