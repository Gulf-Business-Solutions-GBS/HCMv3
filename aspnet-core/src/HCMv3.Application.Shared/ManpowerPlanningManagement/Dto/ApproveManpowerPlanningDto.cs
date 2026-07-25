namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Input for <c>ManpowerPlanningsAppService.Approve</c>/<c>Reject</c>. Ported from the legacy
/// <c>ManpowerPlanning.OnSaving</c> "Entry On Final Approval" block, which created a new <see cref="Position"/>
/// for every <see cref="ManpowerPlanningDetail"/> line without one once <c>RequestStatus</c> became
/// <c>Approved</c>.
/// </summary>
public class ApproveManpowerPlanningDto
{
    public int Id { get; set; }
}
