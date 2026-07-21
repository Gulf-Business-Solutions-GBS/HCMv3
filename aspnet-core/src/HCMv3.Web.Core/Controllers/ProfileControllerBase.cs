using Abp.AspNetZeroCore.Net;
using Abp.Extensions;
using Abp.IO.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HCMv3.Authorization.Users.Profile;
using HCMv3.Dto;
using HCMv3.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HCMv3.Web.Controllers;

public abstract class ProfileControllerBase : HCMv3ControllerBase
{
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly IProfileAppService _profileAppService;

    protected ProfileControllerBase(
        ITempFileCacheManager tempFileCacheManager,
        IProfileAppService profileAppService)
    {
        _tempFileCacheManager = tempFileCacheManager;
        _profileAppService = profileAppService;
    }

    public void UploadProfilePictureFile(FileDto input)
    {
        var profilePictureFile = Request.Form.Files.First();

        //Check input
        if (profilePictureFile == null)
        {
            throw new UserFriendlyException(L("ProfilePicture_Change_Error"));
        }

        using (var stream = profilePictureFile.OpenReadStream())
        {
            byte[] fileBytes;
            fileBytes = stream.GetAllBytes();

            _tempFileCacheManager.SetFile(input.FileToken, fileBytes);
        }

    }

    [AllowAnonymous]
    public FileResult GetDefaultProfilePicture()
    {
        return GetDefaultProfilePictureInternal();
    }

    public async Task<FileResult> GetProfilePictureByUser(long userId)
    {
        var output = await _profileAppService.GetProfilePictureByUser(userId);
        if (output.ProfilePicture.IsNullOrEmpty())
        {
            return GetDefaultProfilePictureInternal();
        }

        return File(Convert.FromBase64String(output.ProfilePicture), MimeTypeNames.ImageJpeg);
    }

    protected FileResult GetDefaultProfilePictureInternal()
    {
        return File(Path.Combine("Common", "Images", "default-profile-picture.png"), MimeTypeNames.ImagePng);
    }
}

