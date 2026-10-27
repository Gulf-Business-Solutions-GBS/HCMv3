using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using HCMv3.Workflow.Services;

namespace HCMv3.Workflow.BusinessRules;

/// <summary>
/// Exposes approval-chain selection to the rule engine, for every workflow transaction type at once.
///
/// <para>
/// Deliberately NOT one base object per transaction type. The requirement that prompted this -- "leave
/// approvers change when the applier is a manager, and again when they are the CHRO" -- is a statement
/// about the APPLIER, not about the leave. Modelling it per type would mean a near-identical base object
/// for each of the fifty transaction types, fifty copies of the same rule, and fifty places to change when
/// the CHRO's job title code changes. One base object with the request type as a condition expresses the
/// same rules and expresses cross-type policy ("anything from a board member routes to the board chain")
/// that per-type objects could not express at all.
/// </para>
///
/// <para>
/// The trade-off is that a routing rule cannot see the transaction's own fields -- it cannot say "leave
/// longer than 20 days". That is the right boundary anyway: chain selection happens before the request is
/// built, and a rule reading a half-built record is how routing becomes unpredictable. Amount-dependent
/// routing belongs on the request's own validation, not on chain choice.
/// </para>
/// </summary>
public class WorkflowRoutingRuleBaseObjectProvider : IRuleBaseObjectProvider, ISingletonDependency
{
    /// <summary>The base object code stored in <c>BusinessRule.BaseObject</c>. Never rename.</summary>
    public const string BaseObjectCode = "WorkflowRouting";

    public const string RequestParameter = "Request";
    public const string ApplierParameter = "Applier";
    public const string RoutingParameter = "Routing";

    public IEnumerable<RuleBaseObjectDescriptor> GetBaseObjects()
    {
        yield return RuleBaseObjectBuilder<WorkflowRequest>
            .For(BaseObjectCode, "WorkflowRouting")

            // Routing only. Offering this on BeforeSave would show authors a Routing parameter nothing
            // reads, and a rule that appears to work while changing nothing is the worst outcome available.
            .SupportsEvents(RuleEventType.OnRoute)

            .Parameter<RuleWorkflowRequestSnapshot>(RequestParameter, "Request", isWritable: false, p => p
                .Read("RequestType", "RequestType", RuleDataType.String, r => r.RequestType)
                .Read("RequestSubTypeId", "RequestSubType", RuleDataType.Lookup, r => r.RequestSubTypeId)
                .Read("CustomSubTypeId", "CustomSubType", RuleDataType.Lookup, r => r.CustomSubTypeId))

            .Parameter<RuleEmployeeSnapshot>(ApplierParameter, "Applier", isWritable: false, p => p
                .Read("EmployeeId", "Employee", RuleDataType.Integer, e => (long)e.EmployeeId)
                .Read("Code", "Code", RuleDataType.String, e => e.Code)
                .Read("JoiningDate", "JoiningDate", RuleDataType.Date, e => e.JoiningDate)
                .Read("Status", "Status", RuleDataType.Enum, e => (long)e.Status, nameof(EmployeeStatus))
                .Read("JobTitleId", "JobTitle", RuleDataType.Lookup, e => e.JobTitleId, "JobTitle")
                .Read("JobTitleCode", "JobTitleCode", RuleDataType.String, e => e.JobTitleCode)
                .Read("EmployeeGroupId", "EmployeeGroup", RuleDataType.Lookup, e => e.EmployeeGroupId, "EmployeeGroup")
                .Read("EmployeeSubGroupId", "EmployeeSubGroup", RuleDataType.Lookup, e => e.EmployeeSubGroupId, "EmployeeSubGroup")
                .Read("OrganizationUnitId", "OrganizationUnit", RuleDataType.Integer, e => e.OrganizationUnitId)
                .Read("CostCenterId", "CostCenter", RuleDataType.Lookup, e => e.CostCenterId, "CostCenter")
                .Read("WorkLocationId", "WorkLocation", RuleDataType.Lookup, e => e.WorkLocationId, "WorkLocation")
                .Read("WorkflowGroupId", "WorkflowGroup", RuleDataType.Lookup, e => e.WorkflowGroupId, "WorkflowGroup")
                .Read("IsManager", "IsManager", RuleDataType.Boolean, e => e.IsManager)
                .Read("DirectReportCount", "DirectReportCount", RuleDataType.Integer, e => (long)e.DirectReportCount))

            .Parameter<WorkflowRoutingDecision>(RoutingParameter, "Routing", isWritable: true, p => p
                .ReadWrite(
                    "RequestMasterCode",
                    "ApprovalChain",
                    RuleDataType.String,
                    d => d.RequestMasterCode,
                    (d, v) => d.RequestMasterCode = v,
                    "RequestMaster"))

            .Build();
    }
}

/// <summary>
/// The facts about a request that are known BEFORE its chain is chosen: what kind it is and which subtype.
/// Everything else about the request depends on the chain, so exposing it would be circular.
/// </summary>
public class RuleWorkflowRequestSnapshot
{
    public string RequestType { get; set; }

    public long? RequestSubTypeId { get; set; }

    public long? CustomSubTypeId { get; set; }
}
