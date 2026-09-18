using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Validation;
using Abp.UI;
using Castle.Core.Logging;
using HCMv3.AiAgent.Chat;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Decides which tools the model is offered, and runs the ones it asks for.
///
/// <para>
/// Nothing the model sends reaches a tool unchecked: a tool that was not offered to THIS user in THIS request
/// is refused even if it exists, and every failure -- bad arguments, a permission refusal, a business rule --
/// comes back to the model as a JSON error it can explain, never as an exception that ends the conversation.
/// </para>
/// </summary>
public class AiAgentToolRegistry : ITransientDependency
{
    public static readonly JsonSerializerOptions ResultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Arabic names must reach the model as Arabic, not as \u escapes a small model reads poorly.
        // Safe here: the result is only ever sent to the model, never rendered as HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IReadOnlyList<IAiAgentTool> _tools;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public AiAgentToolRegistry(IEnumerable<IAiAgentTool> tools, IPermissionChecker permissionChecker, IUnitOfWorkManager unitOfWorkManager)
    {
        _tools = tools.OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
        _permissionChecker = permissionChecker;
        _unitOfWorkManager = unitOfWorkManager;

        var duplicate = _tools.GroupBy(t => t.Name, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new InvalidOperationException($"Two AI agent tools are named '{duplicate.Key}'. Tool names must be unique.");
        }
    }

    public IReadOnlyList<IAiAgentTool> AllTools => _tools;

    /// <summary>
    /// The tools this user may be offered. Sorted by name so the request the model sees is identical from one
    /// message to the next, which keeps the host's prompt cache useful.
    /// </summary>
    public async Task<List<IAiAgentTool>> GetAvailableToolsAsync()
    {
        var available = new List<IAiAgentTool>();

        foreach (var tool in _tools)
        {
            if (tool.RequiredPermission == null || await _permissionChecker.IsGrantedAsync(tool.RequiredPermission))
            {
                available.Add(tool);
            }
        }

        return available;
    }

    public static AiToolDefinition ToDefinition(IAiAgentTool tool) => new()
    {
        Name = tool.Name,
        Description = tool.Description,
        Parameters = tool.BuildParameters()
    };

    /// <returns>The JSON handed back to the model: the tool's result, or <c>{"error": "..."}</c>.</returns>
    public async Task<string> ExecuteAsync(AiToolCall call, IReadOnlyCollection<IAiAgentTool> offeredTools, AiToolContext context)
    {
        var tool = offeredTools.FirstOrDefault(t => string.Equals(t.Name, call.Name, StringComparison.Ordinal));

        if (tool == null)
        {
            return Error($"There is no tool named '{call.Name}'. Use only the tools you were given.");
        }

        try
        {
            var arguments = AiToolArguments.Parse(call.ArgumentsJson);
            object result;

            // Each tool call gets its own short unit of work. The conversation deliberately holds none while the
            // model thinks (that can take minutes), and not every service a tool calls opens one for itself -- the
            // admin and self-service lookups did not, and failed with "Value cannot be null (Parameter 'unitOfWork')" on the
            // server. Opening it here, around the tool only, keeps nothing open during the model's wait.
            using (var uow = _unitOfWorkManager.Begin())
            {
                result = await tool.ExecuteAsync(arguments, context);
                await uow.CompleteAsync();
            }

            return Truncate(JsonSerializer.Serialize(result, ResultJsonOptions));
        }
        catch (AiToolArgumentException ex)
        {
            return Error(ex.Message);
        }
        catch (AbpAuthorizationException)
        {
            return Error("The user does not have permission for this.");
        }
        catch (AbpValidationException ex)
        {
            var details = string.Join(" ", ex.ValidationErrors.Select(e => e.ErrorMessage));
            return Error("The request was not valid. " + details);
        }
        catch (UserFriendlyException ex)
        {
            // Business-rule refusals ("not enough balance", "not linked to an employee"). The message is already
            // written for a person, so the model can pass it on.
            return Error(string.IsNullOrWhiteSpace(ex.Details) ? ex.Message : ex.Message + " " + ex.Details);
        }
        catch (Exception ex)
        {
            Logger.Error($"AI agent tool '{call.Name}' failed.", ex);
            return Error("The lookup failed because of a system error. Tell the user to try again later.");
        }
    }

    private static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, ResultJsonOptions);

    private static string Truncate(string json) =>
        json.Length <= AiAgentConsts.MaxToolResultLength
            ? json
            : json.Substring(0, AiAgentConsts.MaxToolResultLength) + "... [truncated]";
}
