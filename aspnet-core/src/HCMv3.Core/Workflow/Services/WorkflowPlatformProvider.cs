using Abp.Dependency;
using Microsoft.AspNetCore.Http;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Reads the platform off the HTTP request in flight.
///
/// <para>
/// Three outcomes, and the ordering is what makes them honest. No HTTP context means nothing a person
/// clicked is responsible for this action -- a background worker, an escalation timer, a migration -- so it
/// is <see cref="WorkflowPlatform.Core"/>. With a context, the mobile app identifies itself by header
/// (<see cref="WorkflowClientHeader"/>) because it calls the very same endpoints the web app does and is
/// otherwise indistinguishable. Everything else is the web client.
/// </para>
///
/// <para>
/// The column was previously written as <c>Core</c> at every site, which made it worse than useless: it
/// looked like a real answer to "where did this come from" and was the same answer every time.
/// </para>
///
/// <para>
/// <see cref="IHttpContextAccessor"/> in a domain service is unusual, and deliberate here. The alternative
/// is threading a platform argument from every application service down through the approval engine, which
/// puts a transport detail into the signature of every domain method it passes and still has to be correct
/// at thirty call sites. Identity does the same thing in this codebase (<c>SignInManager</c>,
/// <c>SecurityStampValidator</c>) for the same reason.
/// </para>
/// </summary>
public class WorkflowPlatformProvider : IWorkflowPlatformProvider, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkflowPlatformProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public WorkflowPlatform Current
    {
        get
        {
            var context = _httpContextAccessor?.HttpContext;

            if (context == null)
            {
                return WorkflowPlatform.Core;
            }

            var client = context.Request?.Headers[WorkflowClientHeader.Name].ToString();

            return string.Equals(client, WorkflowClientHeader.Mobile, System.StringComparison.OrdinalIgnoreCase)
                ? WorkflowPlatform.Mobile
                : WorkflowPlatform.ESS;
        }
    }
}
