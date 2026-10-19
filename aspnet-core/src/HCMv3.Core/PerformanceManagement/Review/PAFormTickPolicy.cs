using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.UI;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>The goal form as it is stored, for <see cref="IPAFormTickPolicy"/>.</summary>
public class PAFormTickSubject
{
    public int PAPeriodId { get; set; }

    public int EmployeeId { get; set; }

    public PAFormBoxValues Values { get; set; } = PAFormBoxValues.Empty();

    public static PAFormTickSubject From(PAForm form) => new()
    {
        PAPeriodId = form.PAPeriodId,
        EmployeeId = form.EmployeeId,
        Values = PAFormBoxValues.From(form)
    };
}

/// <summary>Loads what <see cref="PAFormTickRules"/> needs and applies it, for the screen and for the save.</summary>
public interface IPAFormTickPolicy : IDomainService
{
    /// <summary>The rules for one form as the given employee sees it today. <paramref name="stored"/> is null for a new form.</summary>
    Task<PAFormTickResult> EvaluateAsync(PAFormTickSubject stored, int? actingEmployeeId, DateTime today);

    /// <summary>
    /// Refuses a save that changes any box the rules do not let this employee change. Judged against the
    /// form as STORED, because that is the state the screen offered the boxes from.
    /// </summary>
    Task EnsureChangesAllowedAsync(PAFormTickSubject stored, PAFormBoxValues proposed, int? actingEmployeeId, DateTime today);
}

/// <inheritdoc cref="IPAFormTickPolicy"/>
public class PAFormTickPolicy : HCMv3DomainServiceBase, IPAFormTickPolicy
{
    private readonly IRepository<PATimeline> _timelineRepository;
    private readonly IRepository<PATimelineException> _timelineExceptionRepository;
    private readonly IRepository<PAPeerNominee> _peerNomineeRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public PAFormTickPolicy(
        IRepository<PATimeline> timelineRepository,
        IRepository<PATimelineException> timelineExceptionRepository,
        IRepository<PAPeerNominee> peerNomineeRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _timelineRepository = timelineRepository;
        _timelineExceptionRepository = timelineExceptionRepository;
        _peerNomineeRepository = peerNomineeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task<PAFormTickResult> EvaluateAsync(PAFormTickSubject stored, int? actingEmployeeId, DateTime today)
    {
        var date = today.Date;

        var context = new PAFormTickContext
        {
            IsNew = stored == null,
            HasActingEmployee = actingEmployeeId.HasValue
        };

        if (stored == null || !actingEmployeeId.HasValue)
        {
            return PAFormTickRules.Evaluate(context);
        }

        var actingId = actingEmployeeId.Value;
        var settings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();

        var timelines = _timelineRepository.GetAll()
            .Where(t => t.PAPeriodId == stored.PAPeriodId)
            .Select(t => new { t.ActivityType, t.StartDate, t.EndDate })
            .ToList();

        // The signed-in employee's OWN extensions, whichever side they are on. For an appraiser that is
        // their extension, not the appraisee's -- legacy passed the logged-in manager here, and so does this.
        var extensions = _timelineExceptionRepository.GetAll()
            .Where(x => x.PAPeriodId == stored.PAPeriodId && x.EmployeeId == actingId)
            .Select(x => new { x.ActivityType, x.ExtensionDate })
            .ToList();

        var nominations = _peerNomineeRepository.GetAll()
            .Where(n => n.PAPeriodId == stored.PAPeriodId && n.PeerId == actingId)
            .Select(n => new { n.EmployeeId, n.Submitted })
            .ToList();

        context.Values = stored.Values;
        context.IsAppraisee = stored.EmployeeId == actingId;
        context.EnablePAPeerReview = settings?.EnablePAPeerReview ?? false;
        context.IsPeerNominee = nominations.Count > 0;
        context.PeerReviewSubmitted = nominations.Any(n => n.EmployeeId == stored.EmployeeId && n.Submitted);
        context.TimelineActivityTypes = timelines.Select(t => t.ActivityType).ToHashSet();
        context.IsInWindow = activity =>
            timelines.Any(t => t.ActivityType == activity && t.StartDate.Date <= date && t.EndDate.Date >= date)
            || extensions.Any(x => x.ActivityType == activity && x.ExtensionDate.Date >= date);

        return PAFormTickRules.Evaluate(context);
    }

    public async Task EnsureChangesAllowedAsync(
        PAFormTickSubject stored,
        PAFormBoxValues proposed,
        int? actingEmployeeId,
        DateTime today)
    {
        var rules = await EvaluateAsync(stored, actingEmployeeId, today);
        if (rules.Unrestricted)
        {
            return;
        }

        var before = stored?.Values ?? PAFormBoxValues.Empty();

        var refused = proposed.ChangedFrom(before)
            .Where(box => !rules[box].Enabled)
            .ToList();

        if (refused.Count > 0)
        {
            throw new UserFriendlyException(
                L("GoalFormBoxCannotBeChanged"),
                string.Join(", ", refused.Select(box => L(box.ToString()))));
        }
    }
}
