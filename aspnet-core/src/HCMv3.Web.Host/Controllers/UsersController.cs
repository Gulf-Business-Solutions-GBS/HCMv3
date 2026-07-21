using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.BackgroundJobs;
using HCMv3.Authorization;
using HCMv3.Authorization.Users.Importing;
using HCMv3.DataImporting.Excel;
using HCMv3.Storage;
using HCMv3.Storage.FileValidator;

namespace HCMv3.Web.Controllers;

[AbpMvcAuthorize(AppPermissions.Pages_Administration_Users)]
public class UsersController(IBinaryObjectManager binaryObjectManager, IBackgroundJobManager backgroundJobManager, IFileValidatorManager fileValidatorManager)
    : ExcelImportControllerBase(binaryObjectManager, backgroundJobManager, fileValidatorManager)
{
    public override string ImportExcelPermission => AppPermissions.Pages_Administration_Users_Create;

    public override async Task EnqueueExcelImportJobAsync(ImportFromExcelJobArgs args)
    {
        await BackgroundJobManager.EnqueueAsync<ImportUsersToExcelJob, ImportFromExcelJobArgs>(args);
    }
}