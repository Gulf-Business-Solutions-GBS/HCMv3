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
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.MasterReport.Dto;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.MasterReport;

/// <summary>
/// Asset analysis grid report -- port of legacy <c>MasterReport/uscRptAsset.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 502-641).
///
/// NOT PORTED: the drill-up/drill-down session stack (only "Table1" ever runs), the ColumnChooser JSON
/// persistence, the callback-panel state machine, and the PDF export button. No row-level security --
/// legacy's own override is commented out (<c>Asset</c> has no employee dimension).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_Asset)]
public class AssetReportAppService : GridReportAppServiceBase, IAssetReportAppService
{
    private readonly IRepository<Asset> _assetRepository;

    public AssetReportAppService(IRepository<Asset> assetRepository)
    {
        _assetRepository = assetRepository;
    }

    protected override string LegacyScreenName => "uscRptAsset";

    protected override string ScreenCaption => "Asset";

    [HttpPost]
    public async Task<PagedResultDto<AssetReportRowDto>> GetAssetReport(GetAssetReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);
        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<AssetReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetAssetReportToExcel(GetAssetReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        return await ExportToFileAsync(
            "Asset.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export. Ports
    /// legacy <c>Table1()</c> (uscRptAsset.ascx.cs:568-604).
    /// </summary>
    private IQueryable<AssetReportRowDto> BuildQuery(GetAssetReportInput input)
    {
        var useArabicNames = UseArabicNames;
        var assetTypeCode = input.AssetTypeCode;
        var assetStatus = input.AssetStatus;

        var query = _assetRepository.GetAll()
            .WhereIf(assetStatus.HasValue, a => a.Status == assetStatus.Value)
            .WhereIf(!assetTypeCode.IsNullOrWhiteSpace(), a => a.AssetType.Code == assetTypeCode);

        return query.Select(a => new AssetReportRowDto
        {
            Id = a.Id,
            Code = a.Code,
            NameEn = a.NameEn,
            NameAr = a.NameAr,
            AssetType = a.AssetType == null ? "" : (useArabicNames ? a.AssetType.NameAr : a.AssetType.NameEn),
            Status = a.Status.ToString(),
            AssetAllocationStatus = a.AssetAllocationStatus.ToString(),
            Brand = a.Brand,
            Model = a.Model,
            Year = a.Year.ToString(),
            Color = a.Color,
            VinNumber = a.VIN_Number,
            Serial = a.Serial,
            PlateNumber = a.PlateNumber,
            Barcode = a.Barcode,
            ExpiryDate = a.ExpiryDate,
            Value = a.Value
        });
    }

    private static IReadOnlyList<GridReportColumn<AssetReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<AssetReportRowDto>>
        {
            new("Code", r => r.Code),
            new("NameEn", r => r.NameEn),
            new("NameAr", r => r.NameAr),
            new("AssetType", r => r.AssetType),
            new("Status", r => r.Status),
            new("AssetAllocationStatus", r => r.AssetAllocationStatus),
            new("Brand", r => r.Brand),
            new("Model", r => r.Model),
            new("Year", r => r.Year),
            new("Color", r => r.Color),
            new("VINNumber", r => r.VinNumber),
            new("Serial", r => r.Serial),
            new("PlateNumber", r => r.PlateNumber),
            new("Barcode", r => r.Barcode),
            new("ExpiryDate", r => r.ExpiryDate),
            new("Value", r => r.Value)
        };
    }
}
