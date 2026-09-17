using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Something the AI assistant may ask to have done: look up a balance, list leave requests.
///
/// <para>
/// A tool stands in for a screen, so it must enforce what that screen enforces. Implementations call the
/// ordinary self-service application services through their interfaces -- which carry their own
/// <c>[AbpAuthorize]</c> and resolve the employee from the session -- rather than reading repositories
/// directly. A tool that queries the database itself is a second copy of the rules, and the copy is the one
/// that drifts.
/// </para>
///
/// <para>
/// <b>Naming matters.</b> ABP registers this conventionally, and Castle only binds a class to this interface
/// when the class name ends in <c>AiAgentTool</c>. A tool named anything else registers as itself only, is
/// never offered to the model, and fails silently. <c>AiAgentToolRegistry_Tests</c> guards this.
/// </para>
/// </summary>
public interface IAiAgentTool : ITransientDependency
{
    /// <summary>snake_case, as the model sees it. Unique.</summary>
    string Name { get; }

    /// <summary>
    /// What the model reads to decide when to call this. Written for the model: say what it returns and which
    /// questions it answers, because that is all a small model has to go on.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The permission the user must hold for the tool to be OFFERED. Null means the page permission is enough.
    /// This is about not tempting the model; the real check is still the service the tool calls.
    /// </summary>
    string RequiredPermission { get; }

    /// <summary>JSON Schema for the arguments. Built fresh on each call.</summary>
    JsonObject BuildParameters();

    /// <summary>
    /// Runs the tool. Return a plain object; it is serialised to JSON for the model. Throw
    /// <see cref="AiToolArgumentException"/> for arguments the model got wrong -- the message is shown to the
    /// model so it can correct itself.
    /// </summary>
    Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context);
}

/// <summary>Where a tool is running: whose conversation, and what it left behind for the reply.</summary>
public class AiToolContext
{
    public int ConversationId { get; set; }

    /// <summary>
    /// Set by a tool that prepared something for the user to confirm, so the reply it produced carries the
    /// confirmation card. A later draft in the same message replaces an earlier one.
    /// </summary>
    public int? CreatedPendingActionId { get; set; }
}
