using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetZeroCore.Net;
using Abp.Authorization;
using HCMv3.Dto;
using HCMv3.Ess.Dto;
using HCMv3.Ess.Exporting;
using HCMv3.Storage;

namespace HCMv3.Ess;

/// <summary>
/// Export to Excel for the self-service request lists.
///
/// <para>
/// One service for all of them rather than an exporter per screen: each list is the employee's own rows,
/// already loaded and already filtered on their screen, so the screen sends what it is showing and gets a
/// workbook back. Leave is the exception -- its rows have requests underneath -- and exports itself, from
/// <see cref="IMyLeaveAppService.GetMyLeavesToExcel"/>.
/// </para>
/// </summary>
[AbpAuthorize]
public class EssListExportAppService : HCMv3AppServiceBase, IEssListExportAppService
{
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EssListExportAppService(ITempFileCacheManager tempFileCacheManager)
    {
        _tempFileCacheManager = tempFileCacheManager;
    }

    public Task<FileDto> ExportToExcel(EssListExportInput input)
    {
        using var book = EssListExcel.BuildFlat(input);
        using var stream = new MemoryStream();
        book.SaveAs(stream);

        var file = new FileDto(FileNameOf(input.Title), MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        _tempFileCacheManager.SetFile(file.FileToken, stream.ToArray());

        return Task.FromResult(file);
    }

    /// <summary>The title as a file name: whatever a file system would refuse is left out.</summary>
    internal static string FileNameOf(string title)
    {
        var clean = new string((title ?? string.Empty).Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();

        return (clean.Length == 0 ? "Export" : clean) + ".xlsx";
    }
}
