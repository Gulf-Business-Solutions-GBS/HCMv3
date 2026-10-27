using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.Integrations.Services;
using HCMv3.PayrollManagement.Costing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// What must hold before a JV may go to Dynamics 365. Checked twice: by the Post JV screen before it queues
/// the run, so the operator is told at once, and again by the run itself just before the first journal,
/// because the run starts later and the data can change in between.
///
/// <para>Legacy checked only the FI links (<c>uscCostingFile.ascx.cs</c> lines 784-788). Its "Validate JV"
/// button (lines 1710-1728) was advisory: Post JV did not run it, and the HCMv3 screen's own gate lived only
/// in the browser. The balance test here is legacy's, verbatim -- both totals rounded to WHOLE units with
/// <c>Math.Round</c> (banker's rounding) and then compared (<see cref="JournalVoucherResult.IsBalanced"/>).</para>
/// </summary>
public class PostJvPreconditions : AbpServiceBase, ITransientDependency
{
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IJournalVoucherBuilder _journalVoucherBuilder;
    private readonly ID365FinancePostingSwitch _postingSwitch;

    public PostJvPreconditions(
        IRepository<PayrollArea> payrollAreaRepository,
        IJournalVoucherBuilder journalVoucherBuilder,
        ID365FinancePostingSwitch postingSwitch)
    {
        _payrollAreaRepository = payrollAreaRepository;
        _journalVoucherBuilder = journalVoucherBuilder;
        _postingSwitch = postingSwitch;
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    /// <summary>Legacy lines 784-788: the area must exist and have both FI links.</summary>
    public Task<PayrollArea> RequirePostableAreaAsync(string payrollAreaName)
    {
        var payrollArea = _payrollAreaRepository.GetAll().FirstOrDefault(a => a.Name == payrollAreaName);

        if (payrollArea == null)
        {
            throw new UserFriendlyException(L("PayrollAreaNotFound"));
        }

        if (payrollArea.FICompanyId == null)
        {
            throw new UserFriendlyException(L("PayrollFICompanyNull"));
        }

        if (payrollArea.FIBankId == null)
        {
            throw new UserFriendlyException(L("PayrollFIBankNull"));
        }

        return Task.FromResult(payrollArea);
    }

    /// <summary>The Integration Center switch for Dynamics 365 Finance.</summary>
    public async Task EnsurePostingSwitchedOnAsync()
    {
        if (!await _postingSwitch.IsPostingAllowedAsync())
        {
            throw new UserFriendlyException(L("D365FinancePostingSwitchedOff"));
        }
    }

    /// <summary>
    /// Builds the JV exactly as the preview did and refuses it when it is empty, does not balance, or is not
    /// the JV the operator previewed.
    /// </summary>
    public async Task<JournalVoucherResult> BuildPostableAsync(CostingRunInput input, string previewFingerprint)
    {
        var result = await _journalVoucherBuilder.BuildAsync(input);

        var refusal = FindRefusal(input, result, previewFingerprint);
        if (refusal != null)
        {
            throw new UserFriendlyException(L(refusal));
        }

        return result;
    }

    /// <summary>The localization key of the first reason not to post, or <c>null</c> when posting may go ahead.</summary>
    public static string FindRefusal(CostingRunInput input, JournalVoucherResult result, string previewFingerprint)
    {
        if (string.IsNullOrWhiteSpace(previewFingerprint))
        {
            return "JournalVoucherPreviewRequired";
        }

        if (result == null || !result.Lines.Any())
        {
            return "NoDataFoundToPostJV";
        }

        if (!result.IsBalanced)
        {
            return "DebitCreditNotMatched";
        }

        if (!JournalVoucherFingerprint.Matches(previewFingerprint, JournalVoucherFingerprint.Compute(input, result)))
        {
            return "JournalVoucherPreviewChanged";
        }

        return null;
    }
}
