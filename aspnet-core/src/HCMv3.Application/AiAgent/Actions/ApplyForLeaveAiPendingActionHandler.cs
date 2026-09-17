using System;
using System.Threading.Tasks;
using HCMv3.Ess;
using HCMv3.Ess.Dto;

namespace HCMv3.AiAgent.Actions;

/// <summary>Submits a leave application the assistant drafted, through the same service as the My Leave form.</summary>
public class ApplyForLeaveAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "ApplyForLeave";

    private readonly IMyLeaveAppService _myLeaveAppService;

    public ApplyForLeaveAiPendingActionHandler(IMyLeaveAppService myLeaveAppService)
    {
        _myLeaveAppService = myLeaveAppService;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        return new AiPendingActionPreview { Title = L("AiAgentActionApplyForLeave") }
            .Add(L("AiAgentLeaveType"), PickName(payload.LeaveTypeNameEn, payload.LeaveTypeNameAr))
            .Add(L("AiAgentFrom"), FormatDate(payload.StartDate))
            .Add(L("AiAgentTo"), FormatDate(payload.EndDate))
            .Add(L("AiAgentDays"), FormatNumber(payload.Days))
            .Add(L("AiAgentComments"), payload.Comments);
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        var result = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = payload.AbsenceTypeId,
            StartDate = payload.StartDate,
            EndDate = payload.EndDate,
            Comments = payload.Comments
        });

        return L(result.SubmittedForApproval ? "AiAgentLeaveSubmittedForApproval" : "AiAgentLeaveRecorded");
    }

    /// <summary>
    /// What will be submitted. The names are a snapshot for the card only -- the id is what is sent -- so the
    /// card still reads correctly if the leave type is renamed before the user confirms.
    /// </summary>
    public class Payload
    {
        public int AbsenceTypeId { get; set; }

        public string LeaveTypeNameEn { get; set; }

        public string LeaveTypeNameAr { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public double Days { get; set; }

        public string Comments { get; set; }
    }
}
