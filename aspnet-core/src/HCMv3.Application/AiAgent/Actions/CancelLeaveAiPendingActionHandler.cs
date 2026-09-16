using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Ess;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Withdraws one of the user's own leave requests, through <see cref="IMyLeaveAppService.CancelMyLeave"/> --
/// which re-checks ownership and that the request is still withdrawable at the moment of confirming, not at
/// the moment the draft was made.
/// </summary>
public class CancelLeaveAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "CancelLeave";

    private readonly IMyLeaveAppService _myLeaveAppService;

    public CancelLeaveAiPendingActionHandler(IMyLeaveAppService myLeaveAppService)
    {
        _myLeaveAppService = myLeaveAppService;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        return new AiPendingActionPreview { Title = L("AiAgentActionCancelLeave") }
            .Add(L("AiAgentLeaveType"), PickName(payload.LeaveTypeNameEn, payload.LeaveTypeNameAr))
            .Add(L("AiAgentFrom"), FormatDate(payload.StartDate))
            .Add(L("AiAgentTo"), FormatDate(payload.EndDate))
            .Add(L("AiAgentDays"), FormatNumber(payload.Days));
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        await _myLeaveAppService.CancelMyLeave(new EntityDto(payload.EmployeeAbsenceId));

        return L("AiAgentLeaveCancelled");
    }

    public class Payload
    {
        public int EmployeeAbsenceId { get; set; }

        public string LeaveTypeNameEn { get; set; }

        public string LeaveTypeNameAr { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public double Days { get; set; }
    }
}
