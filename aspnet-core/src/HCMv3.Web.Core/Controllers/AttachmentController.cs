using System;
using System.Collections.Generic;
using System.Linq;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.IO.Extensions;
using Abp.UI;
using Abp.Web.Models;
using HCMv3.Storage;
using HCMv3.Storage.FileValidator;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Web.Controllers;

/// <summary>
/// First half of adding an attachment: receives the multipart upload and parks it in the temp file cache.
/// The second half, <c>AttachmentAppService.AddAttachment</c>, checks who may attach to which record and the
/// tenant's limits, then stores it. Split the ASP.NET Zero way (as the profile picture is) because an
/// application service cannot take a multipart file, and base64 in JSON would inflate every upload by a third.
/// </summary>
public class AttachmentController : HCMv3ControllerBase
{
    /// <summary>
    /// Hard ceiling regardless of tenant settings, so an oversized body is refused before it is held in
    /// memory. The tenant's own (usually smaller) limit is applied when the file is attached.
    /// </summary>
    private const long AbsoluteMaxBytes = 50L * 1024 * 1024;

    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly IEnumerable<IFileValidator> _fileValidators;

    public AttachmentController(ITempFileCacheManager tempFileCacheManager, IEnumerable<IFileValidator> fileValidators)
    {
        _tempFileCacheManager = tempFileCacheManager;
        _fileValidators = fileValidators;
    }

    [HttpPost]
    [AbpMvcAuthorize]
    [RequestSizeLimit(AbsoluteMaxBytes + 1024 * 1024)]
    public JsonResult UploadFile()
    {
        try
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                throw new UserFriendlyException(L("AttachmentIsEmpty"));
            }

            if (file.Length > AbsoluteMaxBytes)
            {
                throw new UserFriendlyException(L("AttachmentTooLarge", AbsoluteMaxBytes / (1024 * 1024)));
            }

            // Content must match the extension for every type the system knows how to check (images, PDF,
            // Office, text). Types with no checker are still limited by the tenant's extension list.
            var input = new FileValidateInput(file);
            var validator = _fileValidators.FirstOrDefault(v => v.CanValidate(input));
            if (validator != null)
            {
                try
                {
                    validator.Validate(input);
                }
                catch (Exception ex)
                {
                    throw new UserFriendlyException(ex.Message);
                }
            }

            byte[] bytes;
            using (var stream = file.OpenReadStream())
            {
                bytes = stream.GetAllBytes();
            }

            var token = Guid.NewGuid().ToString("N");
            _tempFileCacheManager.SetFile(token, new TempFileInfo(file.FileName, file.ContentType, bytes));

            return Json(new AjaxResponse(new { fileToken = token, fileName = file.FileName, size = file.Length }));
        }
        catch (UserFriendlyException ex)
        {
            return Json(new AjaxResponse(new ErrorInfo(ex.Message)));
        }
    }
}
