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
/// Employee Asset analysis grid report -- port of legacy
/// <c>uscRptEmployeeAsset.ascx(.cs)</c>, method <c>Table1()</c> (lines 462-660).
///
/// NOT PORTED (DevExpress/WebForms UI plumbing, no data meaning): the drill-up/drill-down session stack
/// (single level here -- <c>GetKey()</c>/<c>GetTable()</c> define only "Table1"), the ColumnChooser JSON
/// persistence, the callback-panel state machine, the PDF export button, and the dead
/// <c>Table1_Old()</c> (lines 661+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeAsset)]
public class EmployeeAssetReportAppService : GridReportAppServiceBase, IEmployeeAssetReportAppService
{
    private readonly IRepository<EmployeeAsset> _employeeAssetRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public EmployeeAssetReportAppService(
        IRepository<EmployeeAsset> employeeAssetRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _employeeAssetRepository = employeeAssetRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeAsset";

    protected override string ScreenCaption => "Employee Asset";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAssetReportRowDto>> GetEmployeeAssetReport(
        GetEmployeeAssetReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeAssetReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeAssetReportToExcel(GetEmployeeAssetReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeAsset.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeAsset.ascx.cs:462-660).
    /// </summary>
    private async Task<IQueryable<EmployeeAssetReportRowDto>> BuildQueryAsync(GetEmployeeAssetReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Legacy line 551: the org-assignment lookup is keyed on DateTime.Now, NOT on the report dates.
        var today = Today;

        var query = _employeeAssetRepository.GetAll()
            // Legacy lines 549-550: !(StartDate > dtTo || EndDate < dtFrom) -- an overlap test.
            .Where(a => a.StartDate.Date <= toDate && a.EndDate.Date >= fromDate)
            // Legacy lines 555-559: Employee.Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, a => a.Employee.Status == employeeStatus.Value)
            // Legacy lines 560-564: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, a => employeeCodes.Contains(a.Employee.Code))
            // Legacy line 566 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId));

        return query.Select(a => new EmployeeAssetReportRowDto
        {
            Id = a.Id,
            EmployeeCode = a.Employee.Code,
            EmployeeName = useArabicNames ? a.Employee.FullNameAr : a.Employee.FullNameEn,
            StartDate = a.StartDate,
            EndDate = a.EndDate,
            // Legacy OrgLevel1..7 (lines 568-580) collapsed to one OrganizationUnit column -- see the DTO
            // remarks. Still resolved on the assignment current TODAY, exactly as legacy did.
            OrganizationUnitName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == a.EmployeeId
                            && o.StartDate <= today
                            && o.EndDate >= today
                            && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnit.DisplayName)
                .FirstOrDefault(),
            // Legacy lines 596-609: every asset field is null-guarded to "" / 0.
            AssetName = a.Asset == null
                ? string.Empty
                : (useArabicNames ? a.Asset.NameAr : a.Asset.NameEn),
            AssetTypeName = a.Asset == null || a.Asset.AssetType == null
                ? string.Empty
                : (useArabicNames ? a.Asset.AssetType.NameAr : a.Asset.AssetType.NameEn),
            Brand = a.Asset == null ? string.Empty : a.Asset.Brand,
            Model = a.Asset == null ? string.Empty : a.Asset.Model,
            Year = a.Asset == null ? string.Empty : a.Asset.Year.ToString(),
            Color = a.Asset == null ? string.Empty : a.Asset.Color,
            VinNumber = a.Asset == null ? string.Empty : a.Asset.VIN_Number,
            Serial = a.Asset == null ? string.Empty : a.Asset.Serial,
            PlateNumber = a.Asset == null ? string.Empty : a.Asset.PlateNumber,
            Barcode = a.Asset == null ? string.Empty : a.Asset.Barcode,
            AssetStatus = a.Asset == null ? string.Empty : a.Asset.Status.ToString(),
            ExpiryDate = a.Asset == null ? default : a.Asset.ExpiryDate,
            Value = a.Asset == null ? 0 : a.Asset.Value
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted (surrogate id,
    /// no meaning to the reader) and the seven OrgLevel columns are the single OrganizationUnit column.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeAssetReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeAssetReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("OrganizationUnit", r => r.OrganizationUnitName),
            new("Asset", r => r.AssetName),
            new("AssetType", r => r.AssetTypeName),
            new("Brand", r => r.Brand),
            new("Model", r => r.Model),
            new("Year", r => r.Year),
            new("Color", r => r.Color),
            new("VINNumber", r => r.VinNumber),
            new("Serial", r => r.Serial),
            new("PlateNumber", r => r.PlateNumber),
            new("Barcode", r => r.Barcode),
            new("Status", r => r.AssetStatus),
            new("ExpiryDate", r => r.ExpiryDate),
            new("Value", r => r.Value)
        };
    }
}
