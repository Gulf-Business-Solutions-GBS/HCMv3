using System;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Storage;
using HCMv3.SystemSecurity;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Writes the bank file's run history -- legacy <c>SysService.AddExecutionLog(..., Module.BankFile, ...)</c>
/// (<c>HCM10.Module/BusinessObjects/Services/SysService.cs:1048</c>), called from the three export paths of
/// <c>uscBankFile.ascx.cs</c> (<c>:2923, :3036, :3276</c>).
///
/// <para><b>No new table.</b> HCMv3 already has legacy's <see cref="ExecutionLog"/> with
/// <see cref="ExecutionLogModule.BankFile"/>; the run is one row of it. Legacy also hung the zipped file off
/// the row through <c>ExecutionLog.DocAttachment</c>. HCMv3 dropped that FK, and its replacement is the
/// generic <see cref="AppAttachment"/>: the copy is attached with owner <see cref="AttachmentEntityType"/> +
/// the log row's id, and its bytes go to the tenant's own folder in the file store -- never into the
/// database.</para>
///
/// <para><b>Failed runs are written too, which legacy did not do.</b> Legacy's popup had a "Failed (n)" filter,
/// but its bank file only ever wrote SUCCESS rows (a failed generate showed an error and wrote nothing), so
/// the filter was always empty. A failed run is written here in its own transaction -- the run's own work is
/// rolled back with the error -- with the error text after the parameters.</para>
/// </summary>
public class BankFileExecutionLogWriter : HCMv3ServiceBase, ITransientDependency
{
    /// <summary>The attachment owner type for a stored copy: the owner is the <see cref="ExecutionLog"/> row.</summary>
    public const string AttachmentEntityType = "ExecutionLog";

    /// <summary>Legacy <c>ScreenCaption</c> for <c>Module.BankFile</c> (SysService.cs:1066), verbatim.</summary>
    public const string ScreenCaption = "Bank File";

    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly IAttachmentManager _attachmentManager;
    private readonly IAbpSession _session;

    public BankFileExecutionLogWriter(
        IRepository<ExecutionLog> executionLogRepository,
        IAttachmentManager attachmentManager,
        IAbpSession session)
    {
        _executionLogRepository = executionLogRepository;
        _attachmentManager = attachmentManager;
        _session = session;
    }

    /// <summary>
    /// The SUCCESS row and the stored copy, in the run's own unit of work -- so they are committed together
    /// with the registers the run locks, as legacy committed them together (<c>LockSalaryRegisters</c>
    /// comment: "changes are automatically committed with ExecutionLog entry").
    /// </summary>
    /// <returns>The log row's id and the stored copy's attachment id (null when it could not be stored).</returns>
    public async Task<(int ExecutionLogId, int? AttachmentId)> WriteSuccessAsync(
        string userName, string parameters, int recordCount, string zipFileName, byte[] zipContent)
    {
        var logId = await _executionLogRepository.InsertAndGetIdAsync(NewRow(
            userName, parameters, ExecutionLogStatus.SUCCESS, recordCount, recordCount, 0));

        int? attachmentId = null;
        if (zipContent != null && zipContent.Length > 0 && !string.IsNullOrWhiteSpace(zipFileName))
        {
            try
            {
                var attachment = await _attachmentManager.AddAsync(
                    AttachmentEntityType, logId.ToString(), zipFileName, zipContent);
                attachmentId = attachment.Id;
            }
            catch (UserFriendlyException ex)
            {
                // The tenant's document limits (allowed types, size, storage) are for files people upload. The
                // payroll officer already has the file in hand, so a refused copy must not undo the run --
                // legacy did not check these limits at all. Logged so an administrator can see why.
                Logger.Warn("Bank file copy for execution log " + logId + " was not stored: " + ex.Message);
            }
        }

        return (logId, attachmentId);
    }

    /// <summary>
    /// A FAILED row, committed on its own because the run's unit of work is about to roll back. Never throws:
    /// failing to log must not hide the error the user needs to see.
    /// </summary>
    public async Task WriteFailedAsync(string userName, string parameters, string error)
    {
        try
        {
            using var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);

            var text = string.IsNullOrWhiteSpace(error)
                ? parameters
                : (string.IsNullOrEmpty(parameters) ? string.Empty : parameters + " | ") + "Error : " + error;

            await _executionLogRepository.InsertAsync(NewRow(userName, text, ExecutionLogStatus.FAILED, 0, 0, 0));
            await uow.CompleteAsync();
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not write the failed bank file execution log: " + ex.Message, ex);
        }
    }

    private ExecutionLog NewRow(string userName, string parameters, ExecutionLogStatus status,
                                int total, int successful, int failed)
    {
        return new ExecutionLog
        {
            TenantId = _session.GetTenantId(),
            UserName = userName,
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.GENERATE,
            Module = ExecutionLogModule.BankFile,
            ScreenName = nameof(BankFilesAppService),
            ScreenCaption = ScreenCaption,
            Parameters = parameters,
            ExecutionStatus = status,
            TotalRecords = total,
            Successfull = successful,
            Failed = failed
        };
    }
}
