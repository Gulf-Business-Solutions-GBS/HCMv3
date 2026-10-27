using System.Collections.Generic;

namespace HCMv3.PerformanceManagement.Dto;

/// <summary>One of the twelve goal-form boxes, as the signed-in employee may use it.</summary>
public class PAFormBoxRuleDto
{
    /// <summary>The <c>PAForm</c> property name, e.g. "PlanSubmitByAppraisee".</summary>
    public string Box { get; set; }

    public bool Visible { get; set; }

    public bool Enabled { get; set; }
}

/// <summary>
/// Which of a goal form's twelve boxes the signed-in employee may see and tick right now (legacy order,
/// side, time window and timeline rules). The server refuses any save that changes a box this says is not
/// enabled.
/// </summary>
public class PAFormTickRulesDto
{
    /// <summary>The account has no employee record, so no rule applies -- legacy's behaviour for the admin.</summary>
    public bool Unrestricted { get; set; }

    /// <summary>The stage open now: Plan, Quarter1, MidYear, Quarter3, EndOfYear or SignOff. Empty for a new form.</summary>
    public string Phase { get; set; }

    public List<PAFormBoxRuleDto> Boxes { get; set; } = new();

    /// <summary>The signed-in peer's own "Submitted" box is shown.</summary>
    public bool ShowPeerSubmitted { get; set; }

    /// <summary>The signed-in peer's own "Submitted" box may be ticked.</summary>
    public bool CanTickPeerSubmitted { get; set; }

    /// <summary>The boxes the goal-form LIST hides for the tenant's appraisal cycle.</summary>
    public List<string> HiddenListBoxes { get; set; } = new();
}
