using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;

namespace HCMv3.ImportExport;

/// <inheritdoc cref="IProfilePhotoImportService"/>
public class ProfilePhotoImportService : HCMv3DomainServiceBase, IProfilePhotoImportService
{
    private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".jepg", ".png", ".gif" };

    /// <summary>Legacy: <c>maxSize = (maxSize == 0 ? 100 : maxSize)</c> KB (comment: "default 100 kb if not
    /// specified -- Adnan 13-09-2023").</summary>
    private const int DefaultMaxPhotoSizeKb = 100;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<BulkUploadLog> _bulkUploadLogRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IEntityImageStore _entityImageStore;

    public ProfilePhotoImportService(
        IRepository<Employee> employeeRepository,
        IRepository<BulkUploadLog> bulkUploadLogRepository,
        IRepository<ExecutionLog> executionLogRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        IEntityImageStore entityImageStore)
    {
        _employeeRepository = employeeRepository;
        _bulkUploadLogRepository = bulkUploadLogRepository;
        _executionLogRepository = executionLogRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _unitOfWorkManager = unitOfWorkManager;
        _entityImageStore = entityImageStore;
    }

    public bool IsSupportedImage(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return SupportedExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<BulkImportRunResult> ImportAsync(ProfilePhotoImportInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var startedAt = Clock.Now;

        // Legacy: `Directory.GetFiles(folder, "*.*").Select(fn => new FileInfo(fn)).OrderBy(f => f.Name)`
        // then an in-loop extension filter. NoOfEmployee was set from the UNFILTERED count, so the progress
        // denominator included non-image files; here the count is the filtered one, which is what the
        // operator actually expects to see.
        var files = (input.Files ?? new List<ProfilePhotoFile>())
            .Where(f => f != null && IsSupportedImage(f.FileName))
            .OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = files.Count;

        var maxPhotoSizeKb = DefaultMaxPhotoSizeKb;
        var personnelSettings = await _sysSettingsAccessor.GetPersonnelSettingsAsync();
        if (personnelSettings != null && personnelSettings.MaxPhotoSize != 0)
        {
            maxPhotoSizeKb = personnelSettings.MaxPhotoSize;
        }

        var executionLogId = await CreateExecutionLogAsync(input.TenantId, total, startedAt);

        var succeeded = 0;
        var failed = 0;
        var aborted = false;

        foreach (var file in files)
        {
            if (await IsStopRequestedAsync(input.TenantId))
            {
                aborted = true;
                break;
            }

            var employeeCode = Path.GetFileNameWithoutExtension(file.FileName);
            int? employeeId = null;
            string message;
            int status;

            try
            {
                await RunInOwnUnitOfWorkAsync(input.TenantId, async () =>
                {
                    var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.Code == employeeCode);

                    if (employee == null)
                    {
                        // Legacy message, verbatim.
                        throw new BulkImportExportException(
                            string.Format(CultureInfo.InvariantCulture, "Employee not found for Code : {0}", employeeCode));
                    }

                    employeeId = employee.Id;

                    var content = file.Content ?? Array.Empty<byte>();
                    if (content.Length > 1024 * maxPhotoSizeKb)
                    {
                        // Legacy message, verbatim.
                        throw new BulkImportExportException(
                            string.Format(CultureInfo.InvariantCulture, "Image Size is bigger than allowed limit for Employee Code : {0}", employeeCode));
                    }

                    // Kept on disk under the tenant's folder, never in the database.
                    employee.PhotoPath = await _entityImageStore.ReplaceAsync(
                        EntityImageKinds.EmployeePhoto, employee.TenantId, employee.Id, employee.PhotoPath, content);
                    employee.LegacyPhoto = null;
                    await _employeeRepository.UpdateAsync(employee);
                });

                status = 0;
                message = "Successfull"; // Legacy literal.
                succeeded++;
            }
            catch (Exception ex)
            {
                status = 1;
                message = ex.Message;
                failed++;
            }

            await SaveLogAsync(input.TenantId, employeeId, message, status, total);
        }

        var result = new BulkImportRunResult
        {
            ExecutionLogId = executionLogId,
            TotalRecords = total,
            Successfull = succeeded,
            Failed = failed,
            Aborted = aborted,
            ValidateOnly = false,
            StartedAt = startedAt,
            FinishedAt = Clock.Now
        };

        await CompleteExecutionLogAsync(input.TenantId, executionLogId, result);

        return result;
    }

    private async Task<int> CreateExecutionLogAsync(int tenantId, int total, DateTime startedAt)
    {
        var id = 0;
        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            id = await _executionLogRepository.InsertAndGetIdAsync(new ExecutionLog
            {
                TenantId = tenantId,
                ActionDateTime = startedAt,
                Action = ExecutionLogAction.RUN,
                Module = ExecutionLogModule.BulkImportExport,
                ScreenName = "uscBulkImport",
                ScreenCaption = "Profile Photo",
                Parameters = string.Format(CultureInfo.InvariantCulture, "Files={0}", total),
                ExecutionStatus = ExecutionLogStatus.INPRECESS,
                TotalRecords = total
            });
        });

        return id;
    }

    private async Task CompleteExecutionLogAsync(int tenantId, int executionLogId, BulkImportRunResult result)
    {
        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);
            if (log == null)
            {
                return;
            }

            log.TotalRecords = result.TotalRecords;
            log.Successfull = result.Successfull;
            log.Failed = result.Failed;
            log.ExecutionStatus = result.Aborted
                ? ExecutionLogStatus.ABORTED
                : result.Failed == 0
                    ? ExecutionLogStatus.SUCCESS
                    : result.Successfull == 0
                        ? ExecutionLogStatus.FAILED
                        : ExecutionLogStatus.PARTIAL;

            await _executionLogRepository.UpdateAsync(log);
        });
    }

    private async Task SaveLogAsync(int tenantId, int? employeeId, string message, int status, int total)
    {
        message ??= "";
        var length = message.Length > 1000 ? 1000 : message.Length;

        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            await _bulkUploadLogRepository.InsertAsync(new BulkUploadLog
            {
                TenantId = tenantId,
                LogType = BulkUploadLogType.ProfilePhoto,
                EmployeeId = employeeId,
                Message = message.Substring(0, length),
                Status = status,
                RunTimeStamp = Clock.Now,
                InPeriod = Clock.Now.Date,
                NoOfEmployee = total
            });
        });
    }

    private async Task<bool> IsStopRequestedAsync(int tenantId)
    {
        var stopped = false;
        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            stopped = await _bulkUploadLogRepository.CountAsync(x => x.StopFlag) > 0;
        });

        return stopped;
    }

    private async Task RunInOwnUnitOfWorkAsync(int tenantId, Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                await action();
                await uow.CompleteAsync();
            }
        }
    }
}
