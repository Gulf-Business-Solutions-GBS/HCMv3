using Abp.AspNetCore.Mvc.Authorization;
using Abp.IO.Extensions;
using Abp.UI;
using Abp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using HCMv3.Chat;
using HCMv3.Storage;
using HCMv3.Storage.FileValidator;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HCMv3.Web.Controllers;

public class ChatControllerBase : HCMv3ControllerBase
{
    protected readonly IBinaryObjectManager BinaryObjectManager;
    protected readonly IChatMessageManager ChatMessageManager;
    private readonly IFileValidatorManager _fileValidatorManager;
    public ChatControllerBase(IBinaryObjectManager binaryObjectManager, IChatMessageManager chatMessageManager, IFileValidatorManager fileValidatorManager)
    {
        BinaryObjectManager = binaryObjectManager;
        ChatMessageManager = chatMessageManager;
        _fileValidatorManager = fileValidatorManager;
    }

    [HttpPost]
    [AbpMvcAuthorize]
    public async Task<JsonResult> UploadFile()
    {
        try
        {
            var file = Request.Form.Files.First();

            //Check input
            if (file == null || file.Length == 0)
            {
                throw new UserFriendlyException(L("File_Empty_Error"));
            }

            if (file.Length > 10000000) //10MB
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
            using (var stream = file.OpenReadStream())
            {
                fileBytes = stream.GetAllBytes();
            }

            var fileObject = new BinaryObject(null, fileBytes, $"File uploaded from chat by {AbpSession.UserId}, File name: {file.FileName} {DateTime.UtcNow}");
            using (CurrentUnitOfWork.SetTenantId(null))
            {
                await BinaryObjectManager.SaveAsync(fileObject);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return Json(new AjaxResponse(new
            {
                id = fileObject.Id,
                name = file.FileName,
                contentType = file.ContentType
            }));
        }
        catch (UserFriendlyException ex)
        {
            return Json(new AjaxResponse(new ErrorInfo(ex.Message)));
        }
    }
}

