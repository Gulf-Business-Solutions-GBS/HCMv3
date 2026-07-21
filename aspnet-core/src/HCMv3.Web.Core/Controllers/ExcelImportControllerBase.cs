using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Abp.IO.Extensions;
using Abp.UI;
using Abp.Web.Models;
using HCMv3.Storage;
using Abp.BackgroundJobs;
using Abp.Authorization;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.Web;
using HCMv3.DataImporting.Excel;
using HCMv3.Storage.FileValidator;

namespace HCMv3.Web.Controllers;

public abstract class ExcelImportControllerBase(
    IBinaryObjectManager binaryObjectManager,
    IBackgroundJobManager backgroundJobManager,
    IFileValidatorManager fileValidatorManager)
    : HCMv3ControllerBase
{
    protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;
    protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
    private readonly IFileValidatorManager _fileValidatorManager = fileValidatorManager;
    public abstract string ImportExcelPermission { get; }

    [HttpPost]
    public async Task<JsonResult> ImportFromExcel()
    {
        if (!await PermissionChecker.IsGrantedAsync(ImportExcelPermission))
        {
            throw new AbpAuthorizationException(
                LocalizationManager.GetString(AbpWebConsts.LocalizationSourceName,
                    "DefaultError403")
            );
        }

        try
        {
            var file = Request.Form.Files[0];

            if (file == null)
            {
                throw new UserFriendlyException(L("File_Empty_Error"));
            }

            if (file.Length > 1048576 * 100) //100 MB
            {
                throw new UserFriendlyException(L("File_SizeLimit_Error"));
            }

            // Validate the uploaded file to ensure it meets the allowed file type and signature requirements.
            var validationResult = _fileValidatorManager.ValidateAll(new FileValidateInput(file));

            if (!validationResult.Success)
            {
                throw new UserFriendlyException($"Validation failed: {validationResult.Message}");
            }

            byte[] fileBytes;

            await using (var stream = file.OpenReadStream())
            {
                fileBytes = await stream.GetAllBytesAsync();
            }

            var tenantId = AbpSession.TenantId;
            var fileObject = new BinaryObject(tenantId, fileBytes, $"{DateTime.UtcNow} import from excel file.");

            await BinaryObjectManager.SaveAsync(fileObject);

            var args = new ImportFromExcelJobArgs
            {
                TenantId = tenantId,
                BinaryObjectId = fileObject.Id,
                ExcelImporter = AbpSession.ToUserIdentifier()
            };

            await EnqueueExcelImportJobAsync(args);

            return Json(new AjaxResponse(new { }));
        }
        catch (UserFriendlyException ex)
        {
            return Json(new AjaxResponse(new ErrorInfo(ex.Message)));
        }
    }

    public abstract Task EnqueueExcelImportJobAsync(ImportFromExcelJobArgs args);
}