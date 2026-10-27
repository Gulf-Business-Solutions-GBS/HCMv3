using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for bank detail changes (<see cref="EmployeeBank"/>).
///
/// <para>
/// The type with the highest stakes of the nine, and the clearest case for approval. This row decides where
/// somebody's salary is paid. An unapproved change reaching payroll is how salary diversion fraud works:
/// the account is swapped shortly before a pay run, the money lands elsewhere, and the employee finds out
/// when they are not paid. Keeping the pending row invisible until an approver has agreed is the whole point.
/// </para>
/// <para>
/// For the same reason this type should be configured with a real approval chain rather than left
/// workflow-disabled -- with workflow off, a self-service change takes effect the moment it is saved.
/// </para>
/// </summary>
public class BankWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeBank>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for bank changes.</summary>
    public const string BankRequestType = "EmployeeBank";

    private readonly IRepository<Bank> _bankRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public BankWorkflowTransactionHandler(
        IRepository<EmployeeBank> repository,
        IRepository<Bank> bankRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _bankRepository = bankRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => BankRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var bank = await GetTargetAsync(context);
        if (bank == null)
        {
            return;
        }

        if (bank.EndDate.Date < bank.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // One or the other, not necessarily both: some countries pay on an account number, others on IBAN.
        // Requiring both would block legitimate changes; requiring neither would send a payment nowhere.
        if (bank.AccountNum.IsNullOrWhiteSpace() && bank.IBAN.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AccountNumberOrIbanIsRequired"));
        }
    }

    /// <summary>
    /// The approver's whole job here is to check the destination account, so the summary shows it -- masked
    /// to the last four characters.
    ///
    /// <para>
    /// Masked because this line is stored on the request and shown in an inbox that other approvers, and
    /// later anyone reading the history, can see. The last four is enough to confirm a change against a
    /// payslip or a phone call; the full number in a summary field would spread it further than the one row
    /// that needs it.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var bank = await GetTargetAsync(context);
        if (bank == null)
        {
            return null;
        }

        var bankName = bank.BankId.HasValue
            ? _bankRepository.GetAll().Where(b => b.Id == bank.BankId.Value).Select(b => new { b.NameEn, b.NameAr }).FirstOrDefault()
            : null;

        var identifier = bank.IBAN.IsNullOrWhiteSpace() ? bank.AccountNum : bank.IBAN;
        var masked = MaskAllButLastFour(identifier);

        return new WorkflowSummary(
            $"{bankName?.NameEn ?? "Bank"}, account {masked}",
            $"{bankName?.NameAr ?? "بنك"}، حساب {masked}");
    }

    private static string MaskAllButLastFour(string value)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return "-";
        }

        var trimmed = value.Trim();

        return trimmed.Length <= 4
            ? new string('*', trimmed.Length)
            : new string('*', trimmed.Length - 4) + trimmed.Substring(trimmed.Length - 4);
    }
}
