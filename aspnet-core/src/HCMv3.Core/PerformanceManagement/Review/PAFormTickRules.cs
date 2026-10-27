using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.SystemSettings;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>The twelve progress boxes on a goal form (<see cref="PAForm"/>), named as its properties are.</summary>
public enum PAFormBox
{
    PlanSubmitByAppraiser,
    PlanSubmitByAppraisee,
    Quarter1SubmitByAppraiser,
    Quarter1SubmitByAppraisee,
    MRSubmitByAppraiser,
    MRSubmitByAppraisee,
    Quarter3SubmitByAppraiser,
    Quarter3SubmitByAppraisee,
    ERSubmitByAppraiser,
    ERSubmitByAppraisee,
    AppraiserSignedOff,
    AppraiseeSignedOff
}

/// <summary>Which stage of the goal form is open, in the strict order legacy walked them.</summary>
public enum PAFormPhase
{
    Plan,
    Quarter1,
    MidYear,
    Quarter3,
    EndOfYear,
    SignOff
}

/// <summary>The twelve box values of a goal form, read off the row or off a proposed change to it.</summary>
public class PAFormBoxValues
{
    private readonly Dictionary<PAFormBox, bool> _values;

    private PAFormBoxValues(Dictionary<PAFormBox, bool> values)
    {
        _values = values;
    }

    public bool this[PAFormBox box] => _values.TryGetValue(box, out var value) && value;

    public static PAFormBoxValues Empty() => new(new Dictionary<PAFormBox, bool>());

    public static PAFormBoxValues From(PAForm form)
    {
        return new PAFormBoxValues(new Dictionary<PAFormBox, bool>
        {
            [PAFormBox.PlanSubmitByAppraiser] = form.PlanSubmitByAppraiser,
            [PAFormBox.PlanSubmitByAppraisee] = form.PlanSubmitByAppraisee,
            [PAFormBox.Quarter1SubmitByAppraiser] = form.Quarter1SubmitByAppraiser,
            [PAFormBox.Quarter1SubmitByAppraisee] = form.Quarter1SubmitByAppraisee,
            [PAFormBox.MRSubmitByAppraiser] = form.MRSubmitByAppraiser,
            [PAFormBox.MRSubmitByAppraisee] = form.MRSubmitByAppraisee,
            [PAFormBox.Quarter3SubmitByAppraiser] = form.Quarter3SubmitByAppraiser,
            [PAFormBox.Quarter3SubmitByAppraisee] = form.Quarter3SubmitByAppraisee,
            [PAFormBox.ERSubmitByAppraiser] = form.ERSubmitByAppraiser,
            [PAFormBox.ERSubmitByAppraisee] = form.ERSubmitByAppraisee,
            [PAFormBox.AppraiserSignedOff] = form.AppraiserSignedOff,
            [PAFormBox.AppraiseeSignedOff] = form.AppraiseeSignedOff
        });
    }

    /// <summary>The boxes whose value differs between the two.</summary>
    public IEnumerable<PAFormBox> ChangedFrom(PAFormBoxValues other)
    {
        return Enum.GetValues<PAFormBox>().Where(box => this[box] != other[box]);
    }
}

/// <summary>Everything the tick rules need to know, already loaded.</summary>
public class PAFormTickContext
{
    /// <summary>The form as stored. Ignored for a new form.</summary>
    public PAFormBoxValues Values { get; set; } = PAFormBoxValues.Empty();

    /// <summary>The form has not been saved yet.</summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// The signed-in user has an employee record. Legacy applied every rule inside
    /// <c>if (loggedinEmp != null)</c>, so an account with none -- the admin, by standing rule -- is not
    /// restricted at all.
    /// </summary>
    public bool HasActingEmployee { get; set; }

    /// <summary>The signed-in employee is the appraisee. Anybody else opening the form is treated as the appraiser.</summary>
    public bool IsAppraisee { get; set; }

    /// <summary>Appraisal Settings "Enable PA Peer Review".</summary>
    public bool EnablePAPeerReview { get; set; }

    /// <summary>
    /// The signed-in employee is a nominated peer in this period. Legacy <c>IsPeerNominee</c> asked this for
    /// the PERIOD only, not for this form's employee; kept as is.
    /// </summary>
    public bool IsPeerNominee { get; set; }

    /// <summary>Their peer review of THIS form's employee is already submitted (legacy <c>PAForm.Submitted</c>).</summary>
    public bool PeerReviewSubmitted { get; set; }

    /// <summary>The activity types the period's timeline defines, at any dates.</summary>
    public ISet<ActivityType> TimelineActivityTypes { get; set; } = new HashSet<ActivityType>();

    /// <summary>
    /// Whether the signed-in employee is inside the window for an activity today: a timeline row covering
    /// today, or their own extension that has not yet passed (legacy <c>IsInPATimelineException</c>).
    /// </summary>
    public Func<ActivityType, bool> IsInWindow { get; set; } = _ => false;
}

/// <summary>One box's answer.</summary>
public class PAFormBoxRule
{
    public PAFormBox Box { get; set; }

    public bool Visible { get; set; }

    public bool Enabled { get; set; }
}

/// <summary>The whole form's answer.</summary>
public class PAFormTickResult
{
    /// <summary>No restriction applies -- the account has no employee record.</summary>
    public bool Unrestricted { get; set; }

    /// <summary>The stage currently open, or null for a new form and for an unrestricted account.</summary>
    public PAFormPhase? Phase { get; set; }

    public List<PAFormBoxRule> Boxes { get; set; } = new();

    /// <summary>The peer's own "Submitted" box is shown.</summary>
    public bool ShowPeerSubmitted { get; set; }

    /// <summary>The peer's own "Submitted" box may be ticked.</summary>
    public bool CanTickPeerSubmitted { get; set; }

    public PAFormBoxRule this[PAFormBox box] => Boxes.First(b => b.Box == box);
}

/// <summary>
/// The legacy goal-form rules for its twelve progress boxes, as one pure function: the dialog greys boxes
/// from this answer and the server refuses any change the same answer forbids.
///
/// <para>
/// Ported from legacy <c>PAFormController.ViewController1_CustomApplyAppearance</c>. In summary:
/// </para>
/// <list type="bullet">
/// <item><b>Strict order.</b> Plan, then Quarter 1, Mid-Year and Quarter 3 where the period's timeline has
/// them, then End-of-Year, then sign-off. Only the open stage's boxes can be changed.</item>
/// <item><b>Each side its own box.</b> The appraisee ticks the "by Appraisee" box, anybody else the "by
/// Appraiser" box.</item>
/// <item><b>Only inside the window</b> -- the stage's timeline dates, or a personal extension.</item>
/// <item><b>Stages not in the timeline are hidden</b> (Quarter 1, Mid-Year, Quarter 3 only; Plan and
/// End-of-Year are always required).</item>
/// <item><b>A peer</b> (peer review on, nominated in the period) sees none of the twelve boxes, only their own
/// "Submitted" box during End-of-Year.</item>
/// </list>
///
/// <para>
/// <b>Legacy oddities kept, each pinned by a test:</b> both sign-off boxes are the APPRAISER's -- the code
/// comment says the employee should tick "Appraisee Signed Off", the code does the opposite, and the owner
/// chose to keep it. Sign-off has no time window. And a CLOSED period locks nothing: legacy disabled every box
/// for it and then re-enabled the open stage's boxes a few lines later.
/// </para>
/// </summary>
public static class PAFormTickRules
{
    private static readonly (PAFormPhase Phase, PAFormBox Appraiser, PAFormBox Appraisee, ActivityType? Activity)[] Stages =
    {
        (PAFormPhase.Plan, PAFormBox.PlanSubmitByAppraiser, PAFormBox.PlanSubmitByAppraisee, ActivityType.Plan),
        (PAFormPhase.Quarter1, PAFormBox.Quarter1SubmitByAppraiser, PAFormBox.Quarter1SubmitByAppraisee, ActivityType.Quarter1Review),
        (PAFormPhase.MidYear, PAFormBox.MRSubmitByAppraiser, PAFormBox.MRSubmitByAppraisee, ActivityType.MidYearReview),
        (PAFormPhase.Quarter3, PAFormBox.Quarter3SubmitByAppraiser, PAFormBox.Quarter3SubmitByAppraisee, ActivityType.Quarter3Review),
        (PAFormPhase.EndOfYear, PAFormBox.ERSubmitByAppraiser, PAFormBox.ERSubmitByAppraisee, ActivityType.AnnualReview),
        (PAFormPhase.SignOff, PAFormBox.AppraiserSignedOff, PAFormBox.AppraiseeSignedOff, null)
    };

    /// <summary>The stages that only exist when the period's timeline defines them.</summary>
    private static readonly Dictionary<PAFormPhase, ActivityType> OptionalStages = new()
    {
        [PAFormPhase.Quarter1] = ActivityType.Quarter1Review,
        [PAFormPhase.MidYear] = ActivityType.MidYearReview,
        [PAFormPhase.Quarter3] = ActivityType.Quarter3Review
    };

    public static PAFormTickResult Evaluate(PAFormTickContext context)
    {
        var result = new PAFormTickResult();

        if (!context.HasActingEmployee)
        {
            result.Unrestricted = true;
            result.Boxes = Enum.GetValues<PAFormBox>()
                .Select(box => new PAFormBoxRule { Box = box, Visible = true, Enabled = true })
                .ToList();
            return result;
        }

        var peerViewer = context.EnablePAPeerReview && context.IsPeerNominee;

        result.Boxes = Enum.GetValues<PAFormBox>()
            .Select(box => new PAFormBoxRule { Box = box, Visible = !peerViewer, Enabled = false })
            .ToList();

        if (context.IsNew)
        {
            // Legacy: "initially no period selected, so hide all checkboxes" -- the optional stages are
            // hidden and nothing at all can be ticked on a form that does not exist yet.
            foreach (var stage in Stages.Where(s => OptionalStages.ContainsKey(s.Phase)))
            {
                result[stage.Appraiser].Visible = false;
                result[stage.Appraisee].Visible = false;
            }

            return result;
        }

        foreach (var (phase, activity) in OptionalStages)
        {
            if (!context.TimelineActivityTypes.Contains(activity))
            {
                var stage = Stages.First(s => s.Phase == phase);
                result[stage.Appraiser].Visible = false;
                result[stage.Appraisee].Visible = false;
            }
        }

        var open = Stages.First(stage => IsOpen(stage.Phase, stage.Appraiser, stage.Appraisee, context));
        result.Phase = open.Phase;

        if (open.Phase == PAFormPhase.SignOff)
        {
            // Both sign-off boxes go to the appraiser. Legacy oddity kept by the owner's decision.
            result[PAFormBox.AppraiserSignedOff].Enabled = !context.IsAppraisee;
            result[PAFormBox.AppraiseeSignedOff].Enabled = !context.IsAppraisee;
        }
        else if (context.IsInWindow(open.Activity!.Value))
        {
            result[open.Appraiser].Enabled = !context.IsAppraisee;
            result[open.Appraisee].Enabled = context.IsAppraisee;
        }

        if (peerViewer)
        {
            // Hidden is not editable.
            result.Boxes.ForEach(b => b.Enabled = false);
        }

        if (context.IsPeerNominee)
        {
            var peerMayTick = open.Phase == PAFormPhase.EndOfYear
                              && !context.PeerReviewSubmitted
                              && context.IsInWindow(ActivityType.AnnualReview);

            // Outside End-of-Year the box is merely shown, greyed; during it, only while it can be ticked.
            result.ShowPeerSubmitted = open.Phase != PAFormPhase.EndOfYear || peerMayTick;
            result.CanTickPeerSubmitted = peerMayTick;
        }

        return result;
    }

    /// <summary>
    /// The boxes the goal-form LIST hides for the tenant's appraisal cycle. Legacy hid these columns by
    /// cycle and the detail view's boxes by timeline; both are kept, each where legacy had it.
    /// </summary>
    public static IReadOnlyCollection<PAFormBox> HiddenForCycle(AppraisalCycle cycle)
    {
        return cycle switch
        {
            AppraisalCycle.HalfYearly => new[]
            {
                PAFormBox.Quarter1SubmitByAppraiser, PAFormBox.Quarter1SubmitByAppraisee,
                PAFormBox.Quarter3SubmitByAppraiser, PAFormBox.Quarter3SubmitByAppraisee
            },
            AppraisalCycle.Annually => new[]
            {
                PAFormBox.Quarter1SubmitByAppraiser, PAFormBox.Quarter1SubmitByAppraisee,
                PAFormBox.MRSubmitByAppraiser, PAFormBox.MRSubmitByAppraisee,
                PAFormBox.Quarter3SubmitByAppraiser, PAFormBox.Quarter3SubmitByAppraisee
            },
            _ => Array.Empty<PAFormBox>()
        };
    }

    /// <summary>
    /// A stage is the open one when it is not finished. An optional stage the timeline does not define is
    /// skipped; sign-off is the stage left when everything before it is finished.
    /// </summary>
    private static bool IsOpen(PAFormPhase phase, PAFormBox appraiser, PAFormBox appraisee, PAFormTickContext context)
    {
        if (phase == PAFormPhase.SignOff)
        {
            return true;
        }

        if (OptionalStages.TryGetValue(phase, out var activity) && !context.TimelineActivityTypes.Contains(activity))
        {
            return false;
        }

        return !(context.Values[appraiser] && context.Values[appraisee]);
    }
}
