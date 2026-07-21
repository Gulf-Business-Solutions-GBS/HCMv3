using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using MiniExcelLibs;
using HCMv3.Dto;
using HCMv3.Storage;

namespace HCMv3.DataExporting.Excel.MiniExcel;

public abstract class MiniExcelExcelExporterBase : HCMv3ServiceBase, ITransientDependency
{
    private readonly ITempFileCacheManager _tempFileCacheManager;

    protected MiniExcelExcelExporterBase(ITempFileCacheManager tempFileCacheManager)
    {
        _tempFileCacheManager = tempFileCacheManager;
    }

    protected async Task<FileDto> CreateExcelPackageAsync(string fileName, List<Dictionary<string, object>> items)
    {
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);

        await SaveAsync(items, file);

        return file;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="items"></param>
    /// <param name="file"></param>
    protected virtual async Task SaveAsync(List<Dictionary<string, object>> items, FileDto file)
    {
        using (var stream = new MemoryStream())
        {
            await stream.SaveAsAsync(items);
            _tempFileCacheManager.SetFile(file.FileToken, stream.ToArray());
        }
    }
}