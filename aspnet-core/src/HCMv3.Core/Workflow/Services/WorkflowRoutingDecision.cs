namespace HCMv3.Workflow.Services;

/// <summary>
/// What a routing rule decided: which approval chain a request should follow.
///
/// <para>
/// The writable parameter of every <c>OnRoute</c> base object. Rules cannot reach into the workflow engine;
/// they set a chain CODE here and <see cref="IRequestMasterResolver"/> looks it up, which keeps the engine's
/// invariants -- the chain must exist, be this tenant's, and have steps -- enforced in C# where they always
/// were. A rule naming a chain that does not exist is a configuration error the resolver reports, not a
/// null reference three layers down.
/// </para>
/// </summary>
public class WorkflowRoutingDecision
{
    /// <summary>
    /// The <c>RequestMaster.Code</c> to route to. Null means the rule had nothing to say, and the
    /// resolver's ordinary workflow-group lookup proceeds untouched -- which is what makes arming a routing
    /// rule safe: a rule whose conditions never match changes nothing.
    /// </summary>
    public string RequestMasterCode { get; set; }

    public bool IsResolved => !string.IsNullOrWhiteSpace(RequestMasterCode);
}
