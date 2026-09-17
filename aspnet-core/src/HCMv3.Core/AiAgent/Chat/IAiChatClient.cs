using System.Threading;
using System.Threading.Tasks;

namespace HCMv3.AiAgent.Chat;

/// <summary>
/// Sends a conversation to the language model and returns its next step: either a reply, or tools it wants
/// run.
///
/// <para>
/// The model is treated as untrusted. It never receives a database connection, a permission or a service
/// account -- only the text it is sent here -- and nothing it returns is acted on without going through a
/// tool that checks the calling user's own permissions.
/// </para>
/// </summary>
public interface IAiChatClient
{
    /// <exception cref="AiChatClientException">The model host was unreachable, timed out, or returned
    /// something that is not a chat completion.</exception>
    Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}
