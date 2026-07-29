using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.ImportExport.Dto;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ImportExport;

/// <inheritdoc cref="IBulkImportExportAppService"/>
[AbpAuthorize(AppPermissions.Pages_ImportExport)]
public class BulkImportExportAppService : HCMv3AppServiceBase, IBulkImportExportAppService
{
    private readonly IBulkImportExportService _bulkImportExportService;
    private readonly IProfilePhotoImportService _profilePhotoImportService;
    private readonly ITimeAttendanceIntegrationService _timeAttendanceIntegrationService;
    private readonly IRepository<BulkUploadLog> _bulkUploadLogRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly IBackgroundJobManager _backgroundJobManager;

    public BulkImportExportAppService(
        IBulkImportExportService bulkImportExportService,
        IProfilePhotoImportService profilePhotoImportService,
        ITimeAttendanceIntegrationService timeAttendanceIntegrationService,
        IRepository<BulkUploadLog> bulkUploadLogRepository,
        IRepository<ExecutionLog> executionLogRepository,
        IBackgroundJobManager backgroundJobManager)
    {
        _bulkImportExportService = bulkImportExportService;
        _profilePhotoImportService = profilePhotoImportService;
        _timeAttendanceIntegrationService = timeAttendanceIntegrationService;
        _bulkUploadLogRepository = bulkUploadLogRepository;
        _executionLogRepository = executionLogRepository;
        _backgroundJobManager = backgroundJobManager;
    }

    [AbpAuthorize(AppPermissions.Pages_ImportExport_BulkImportExport)]
    public Task<GetBulkImportEntitiesOutput> GetBulkImportEntities()
    {
        var entities = _bulkImportExportService.GetImportableEntities()
            .Select(e => new BulkImportEntityDto
            {
                Name = e.Name,
                FullName = e.FullName,
                Area = e.Area,
                Columns = e.Columns
            })
            .ToList();

        return Task.FromResult(new GetBulkImportEntitiesOutput { Entities = entities });
    }

    [AbpAuthorize(AppPermissions.Pages_ImportExport_BulkImportExport_Export)]
    public Task<BulkExcelOutput> DownloadBulkExcel(BulkExportInput input)
    {
        var descriptor = _bulkImportExportService.GetEntityDescriptor(input.EntityFullName);
        var bytes = _bulkImportExportService.BuildWorkbook(input.EntityFullName, input.WithData);

        // Legacy: gridExport.FileName = cmbModule.SelectedItem.Text + "_" + DateTime.Now.ToString("yyyy_MM_dd_HHmm")
        var fileName = string.Format(
            CultureInfo.InvariantCulture,
            "{0}_{1}.xlsx",
            descriptor.Name,
            Clock.Now.ToString("yyyy_MM_dd_HHmm", CultureInfo.InvariantCulture));

        return Task.FromResult(new BulkExcelOutput
        {
            FileName = fileName,
            Base64Content = Convert.ToBase64String(bytes)
        });
    }

    [AbpAuthorize(AppPermissions.Pages_ImportExport_BulkImportExport_Import)]
    public async Task<StartBulkImportOutput> StartBulkImport(StartBulkImportInput input)
    {
        // Fail fast on the two checks legacy did up front in Main() (entity/file selected) so the caller
        // gets a 400 rather than a job that dies on the queue. The entity lookup also validates the name.
        var descriptor = _bulkImportExportService.GetEntityDescriptor(input.EntityFullName);
        var content = DecodeBase64(input.FileBase64, L("BulkImportFileNotSelected"));

        // Legacy called TruncateLog() before queueing, so the status panel always reflects the run in
        // progress rather than a mix of this run and the last one.
        await _bulkImportExportService.ClearLogAsync();

        await _backgroundJobManager.EnqueueAsync<BulkImportBackgroundJob, BulkImportJobArgs>(
            new BulkImportJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                EntityFullName = descriptor.FullName,
                FileContent = content,
                FileName = input.FileName,
                ImportType = input.ImportType,
                ValidateOnly = input.ValidateOnly,
                CurrentUserName = (await GetCurrentUserAsync()).UserName
            });

        return new StartBulkImportOutput
        {
            Queued = true,
            // Legacy set lblStatus.Text = LongOperationStatus, whose value at that moment was "NotStarted"
            // or "Process Started"; the equivalent here is a localized "queued" message.
            Message = L("BulkImportQueued", descriptor.Name)
        };
    }

    [AbpAuthorize(AppPermissions.Pages_ImportExport_BulkImportExport_Import)]
    public async Task StopBulkImport()
    {
        await _bulkImportExportService.RequestStopAsync();
    }

    [AbpAuthorize(AppPermissions.Pages_ImportExport_BulkImportExport_Import)]
    public async Task ClearBulkImportStatus()
    {
        await _bulkImportExportService.ClearLogAsync();
    }

    /// <summary>
    /// Direct port of legacy <c>CalculateProcessStatus()</c>, including its speed/ETA arithmetic:
    /// the "current window" is every log row from the last row written more than a minute before the newest
    /// row, up to the newest row; speed is that window's row count scaled to 60 seconds.
    ///
    /// <para>Legacy used <c>Max(r =&gt; r.Oid)</c> on a possibly-empty sequence, which XPO/SQL resolved to
    /// null-&gt;0 but LINQ would throw on. <c>DefaultIfEmpty(0)</c> makes that explicit.</para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ImportExport_BulkImportExport)]
    public async Task<BulkImportStatusOutput> GetBulkImportStatus()
    {
        var query = _bulkUploadLogRepository.GetAll();

        var summaryAll = await query.CountAsync();
        var summarySuccess = await query.CountAsync(r => r.Status == 0);
        var summaryFailed = summaryAll - summarySuccess > 0 ? summaryAll - summarySuccess : 0;

        var output = new BulkImportStatusOutput
        {
            SummaryAll = summaryAll,
            SummarySuccess = summarySuccess,
            SummaryFailed = summaryFailed,
            StopRequested = await query.AnyAsync(r => r.StopFlag)
        };

        var lastExecution = await _executionLogRepository.GetAll()
            .Where(e => e.Module == ExecutionLogModule.BulkImportExport)
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync();

        if (lastExecution != null)
        {
            output.ExecutionStatus = lastExecution.ExecutionStatus.ToString();
        }

        // Legacy `tempLog = allLoglist.Select(r => new { r.Oid, r.NoOfEmployee }).LastOrDefault()`.
        var lastLog = await query.OrderByDescending(r => r.Id).FirstOrDefaultAsync();
        if (lastLog == null)
        {
            return output;
        }

        output.TotalRecords = lastLog.NoOfEmployee;
        output.ProcessedRecords = summaryAll;
        output.LastActivity = lastLog.RunTimeStamp;

        var procEndId = lastLog.Id;
        var procEndTime = lastLog.RunTimeStamp;

        var procStartId = await query
            .Where(r => r.RunTimeStamp < procEndTime.AddMinutes(-1))
            .Select(r => r.Id)
            .DefaultIfEmpty(0)
            .MaxAsync();

        if (procStartId == 0)
        {
            procStartId = lastLog.Id;
        }

        var procStartTime = await query
            .Where(r => r.Id == procStartId)
            .Select(r => r.RunTimeStamp)
            .FirstOrDefaultAsync();

        var windowCount = await query.CountAsync(r => r.Id >= procStartId && r.Id <= procEndId);

        var totalSeconds = procEndTime.Subtract(procStartTime).TotalSeconds;
        if (totalSeconds != 0)
        {
            output.SpeedPerMinute = Math.Round(60 * windowCount / totalSeconds);
            if (output.SpeedPerMinute != 0)
            {
                var remaining = output.TotalRecords - output.ProcessedRecords;
                output.RemainingMinutes = Math.Round(remaining / output.SpeedPerMinute);
            }
        }

        return output;
    }

    /// <summary>
    /// <c>[HttpPost]</c> is required by this repo's NSwag rule: without it ABP would route this
    /// <c>Get*</c> method as HTTP GET, NSwag would flatten <see cref="GetBulkUploadLogEntriesInput"/> into
    /// query-string parameters and never emit the DTO class into <c>service-proxies.ts</c>.
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ImportExport_BulkImportExport)]
    public async Task<PagedResultDto<BulkUploadLogEntryDto>> GetBulkUploadLogEntries(GetBulkUploadLogEntriesInput input)
    {
        var query = _bulkUploadLogRepository.GetAll()
            .WhereIf(input.Status.HasValue, r => r.Status == input.Status.Value);

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "RecordNo" : input.Sorting)
            .PageBy(input)
            .Select(r => new BulkUploadLogEntryDto
            {
                Id = r.Id,
                RecordNo = r.RecordNo,
                RunTimeStamp = r.RunTimeStamp,
                Message = r.Message,
                Status = r.Status,
                EmployeeCode = r.Employee != null ? r.Employee.Code : null,
                EmployeeName = r.Employee != null ? r.Employee.FirstNameEn : null
            })
            .ToListAsync();

        return new PagedResultDto<BulkUploadLogEntryDto>(totalCount, entries);
    }

    [AbpAuthorize(AppPermissions.Pages_ImportExport_ProfilePhotoImport)]
    public async Task<StartBulkImportOutput> StartProfilePhotoImport(StartProfilePhotoImportInput input)
    {
        if (input.Files == null || input.Files.Count == 0)
        {
            throw new UserFriendlyException(L("BulkImportFileNotSelected"));
        }

        var files = new List<ProfilePhotoJobFile>();
        foreach (var file in input.Files)
        {
            if (!_profilePhotoImportService.IsSupportedImage(file.FileName))
            {
                // Legacy silently skipped non-image files found in the folder. An explicitly uploaded file
                // that will never be processed is a different situation, so it is rejected loudly.
                throw new UserFriendlyException(L("BulkImportUnsupportedImage", file.FileName));
            }

            files.Add(new ProfilePhotoJobFile
            {
                FileName = file.FileName,
                Content = DecodeBase64(file.FileBase64, L("BulkImportFileNotSelected"))
            });
        }

        await _bulkImportExportService.ClearLogAsync();

        await _backgroundJobManager.EnqueueAsync<ProfilePhotoImportBackgroundJob, ProfilePhotoImportJobArgs>(
            new ProfilePhotoImportJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                Files = files
            });

        return new StartBulkImportOutput
        {
            Queued = true,
            Message = L("BulkImportQueued", L("ProfilePhotoImport"))
        };
    }

    [AbpAuthorize(AppPermissions.Pages_ImportExport_TimesheetImport)]
    public async Task<ImportTimesheetOutput> ImportTimesheet(ImportTimesheetInput input)
    {
        var content = DecodeBase64(input.FileBase64, L("BulkImportFileNotSelected"));
        var result = await _timeAttendanceIntegrationService.ImportTransactionsFromUploadAsync(content);

        return new ImportTimesheetOutput
        {
            Inserted = result.Inserted,
            Skipped = result.Skipped,
            Failed = result.Failed,
            Errors = result.Errors
        };
    }

    private static byte[] DecodeBase64(string value, string emptyMessage)
    {
        if (value.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(emptyMessage);
        }

        // Browsers commonly hand back a "data:...;base64," prefixed string from FileReader.readAsDataURL.
        var commaIndex = value.IndexOf(',');
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex > 0)
        {
            value = value.Substring(commaIndex + 1);
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new UserFriendlyException(emptyMessage);
        }
    }
}
