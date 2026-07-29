using System.Linq;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;

namespace HCMv3.ImportExport;

/// <summary>
/// The only place a generic bulk import actually runs.
///
/// <para>Replaces legacy <c>uscBulkImportExport.ExecuteLongOperation</c>, which was handed to
/// <c>ThreadPool.QueueUserWorkItem</c> straight from the web request
/// (<c>ASPxCallbackPanel1_Callback</c>). Queueing an ABP background job instead means the run survives an
/// app-pool recycle, is not tied to a request thread, and gets ABP's retry/logging -- legacy's thread-pool
/// work item silently died with the worker process.</para>
///
/// <para>Note: the engine itself opens a nested unit of work per row (see
/// <c>BulkImportExportService.ImportAsync</c>), so unlike the Time Engine job this one deliberately does NOT
/// wrap the whole run in one transaction.</para>
/// </summary>
public class BulkImportBackgroundJob : AsyncBackgroundJob<BulkImportJobArgs>, ITransientDependency
{
    private readonly IBulkImportExportService _bulkImportExportService;

    public BulkImportBackgroundJob(IBulkImportExportService bulkImportExportService)
    {
        _bulkImportExportService = bulkImportExportService;
    }

    public override async Task ExecuteAsync(BulkImportJobArgs args)
    {
        var result = await _bulkImportExportService.ImportAsync(new BulkImportRunInput
        {
            TenantId = args.TenantId,
            EntityFullName = args.EntityFullName,
            FileContent = args.FileContent,
            FileName = args.FileName,
            ImportType = args.ImportType,
            ValidateOnly = args.ValidateOnly,
            CurrentUserName = args.CurrentUserName
        });

        Logger.Info(string.Format(
            "Bulk import finished. Entity={0} ExecutionLogId={1} Total={2} Success={3} Failed={4} Aborted={5} ValidateOnly={6}",
            args.EntityFullName, result.ExecutionLogId, result.TotalRecords,
            result.Successfull, result.Failed, result.Aborted, result.ValidateOnly));
    }
}

/// <summary>
/// The only place a profile-photo import actually runs. Replaces legacy
/// <c>uscBulkImport.ExecuteLongOperation</c> (also a raw <c>ThreadPool.QueueUserWorkItem</c>).
/// </summary>
public class ProfilePhotoImportBackgroundJob : AsyncBackgroundJob<ProfilePhotoImportJobArgs>, ITransientDependency
{
    private readonly IProfilePhotoImportService _profilePhotoImportService;

    public ProfilePhotoImportBackgroundJob(IProfilePhotoImportService profilePhotoImportService)
    {
        _profilePhotoImportService = profilePhotoImportService;
    }

    public override async Task ExecuteAsync(ProfilePhotoImportJobArgs args)
    {
        var result = await _profilePhotoImportService.ImportAsync(new ProfilePhotoImportInput
        {
            TenantId = args.TenantId,
            Files = (args.Files ?? new System.Collections.Generic.List<ProfilePhotoJobFile>())
                .Select(f => new ProfilePhotoFile { FileName = f.FileName, Content = f.Content })
                .ToList()
        });

        Logger.Info(string.Format(
            "Profile photo import finished. ExecutionLogId={0} Total={1} Success={2} Failed={3} Aborted={4}",
            result.ExecutionLogId, result.TotalRecords, result.Successfull, result.Failed, result.Aborted));
    }
}
