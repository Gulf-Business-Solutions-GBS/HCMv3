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
/// Employee Asset Invoice analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeAssetInvoice.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 495-680).
///
/// NOT PORTED (DevExpress/WebForms UI plumbing, no data meaning): the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 686+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeAssetInvoice)]
public class EmployeeAssetInvoiceReportAppService : GridReportAppServiceBase, IEmployeeAssetInvoiceReportAppService
{
    private readonly IRepository<EmployeeAssetInvoice> _employeeAssetInvoiceRepository;
    private readonly IRepository<EmployeeAsset> _employeeAssetRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeAssetInvoiceReportAppService(
        IRepository<EmployeeAssetInvoice> employeeAssetInvoiceRepository,
        IRepository<EmployeeAsset> employeeAssetRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeAssetInvoiceRepository = employeeAssetInvoiceRepository;
        _employeeAssetRepository = employeeAssetRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeAssetInvoice";

    protected override string ScreenCaption => "Employee Asset Invoice";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAssetInvoiceReportRowDto>> GetEmployeeAssetInvoiceReport(
        GetEmployeeAssetInvoiceReportInput input)
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

        return new PagedResultDto<EmployeeAssetInvoiceReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeAssetInvoiceReportToExcel(GetEmployeeAssetInvoiceReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeAssetInvoice.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeAssetInvoice.ascx.cs:572-644).
    /// </summary>
    private async Task<IQueryable<EmployeeAssetInvoiceReportRowDto>> BuildQueryAsync(
        GetEmployeeAssetInvoiceReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var assetTypeCode = input.AssetTypeCode;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var query = _employeeAssetInvoiceRepository.GetAll()
            // Legacy lines 573-574: plain InvoiceDate range (not an overlap test).
            .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
            // Legacy lines 578-581: r.Asset.AssetType.Code == _code.
            .WhereIf(!assetTypeCode.IsNullOrWhiteSpace(), i => i.Asset != null && i.Asset.AssetType != null && i.Asset.AssetType.Code == assetTypeCode)
            // Legacy lines 582-586: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, i => employeeCodes.Contains(i.Employee.Code))
            // Legacy line 588 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, i => rlsEmployeeIds.Contains(i.EmployeeId));

        return query.Select(i => new EmployeeAssetInvoiceReportRowDto
        {
            Id = i.Id,
            EmployeeCode = i.Employee.Code,
            EmployeeName = useArabicNames ? i.Employee.FullNameAr : i.Employee.FullNameEn,
            // Legacy lines 601-622: JobTitle/OrgLevel1..7 from the org assignment current TODAY (not the
            // invoice date).
            JobTitle = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == i.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == i.EmployeeId && o.StartDate <= today && o.EndDate >= today && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            AssetName = i.Asset == null ? string.Empty : (useArabicNames ? i.Asset.NameAr : i.Asset.NameEn),
            AssetTypeName = i.Asset == null || i.Asset.AssetType == null
                ? string.Empty
                : (useArabicNames ? i.Asset.AssetType.NameAr : i.Asset.AssetType.NameEn),
            InvoiceDate = i.InvoiceDate,
            InvoiceAmount = i.InvoiceAmount,
            EmployeePlanAmount = i.EmployeePlanAmount,
            // Legacy lines 632-640: the EmployeeAsset assignment (same Employee+Asset) whose window
            // contains the invoice date.
            AssetDeductionMode = _employeeAssetRepository.GetAll()
                .Where(a => a.EmployeeId == i.EmployeeId && a.AssetId == i.AssetId
                            && a.StartDate <= i.InvoiceDate && a.EndDate >= i.InvoiceDate)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => (AssetDeductionMode?)a.AssetDeductionMode)
                .FirstOrDefault(),
            DeductionAmount = i.DeductionAmount,
            DeductFromEmployee = i.DeductFromEmployee,
            Comments = i.Comments
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeAssetInvoiceReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeAssetInvoiceReportRowDto>>
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
            new("Asset", r => r.AssetName),
            new("AssetType", r => r.AssetTypeName),
            new("InvoiceDate", r => r.InvoiceDate),
            new("InvoiceAmount", r => r.InvoiceAmount),
            new("EmployeePlanAmount", r => r.EmployeePlanAmount),
            new("AssetDeductionMode", r => r.AssetDeductionMode),
            new("DeductionAmount", r => r.DeductionAmount),
            new("DeductFromEmployee", r => r.DeductFromEmployee),
            new("Comments", r => r.Comments)
        };
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows AND applies legacy line 668's
    /// EmployeePlanAmount-zeroing rule (the plan amount is meaningless once the whole invoice is deducted
    /// "Full" in one shot). Shared by the read and the export so the two cannot drift.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeAssetInvoiceReportRowDto> rows)
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

            if (row.AssetDeductionMode == PersonnelAdministration.AssetDeductionMode.Full)
            {
                row.EmployeePlanAmount = 0;
            }
        }
    }
}
