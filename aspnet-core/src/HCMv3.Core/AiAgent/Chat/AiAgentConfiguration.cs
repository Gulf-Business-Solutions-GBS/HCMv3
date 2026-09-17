using System.Globalization;
using Abp.Dependency;
using HCMv3.Configuration;
using Microsoft.Extensions.Configuration;

namespace HCMv3.AiAgent.Chat;

/// <summary>
/// Where the model lives. Read from the <c>AiAgent</c> section of appsettings; every value has a default,
/// so with no section at all the assistant talks to Ollama on the same machine.
///
/// <para>
/// To move to a GPU server or a hosted Qwen endpoint, set <c>AiAgent:BaseUrl</c>, <c>AiAgent:Model</c> and,
/// if the host needs one, <c>AiAgent:ApiKey</c>. No code changes.
/// </para>
/// </summary>
public class AiAgentConfiguration : ITransientDependency
{
    public const string DefaultBaseUrl = "http://localhost:11434/v1";
    public const string DefaultModel = "qwen3:4b";
    // Measured on the dev laptop (no GPU) with qwen3:4b: a leave question took ~70s, an administrator's pay
    // sentence with the full tool list 170-270s. 300 would have cut the slowest answers off, so the default
    // allows for the worst case rather than the typical one; a GPU host answers in seconds and never waits.
    public const int DefaultTimeoutSeconds = 900;
    public const double DefaultTemperature = 0.2;

    private readonly IConfigurationRoot _appConfiguration;

    public AiAgentConfiguration(IAppConfigurationAccessor configurationAccessor)
    {
        _appConfiguration = configurationAccessor.Configuration;
    }

    /// <summary>The OpenAI-compatible base, up to and including <c>/v1</c>.</summary>
    public string BaseUrl => (NonEmpty("AiAgent:BaseUrl") ?? DefaultBaseUrl).TrimEnd('/');

    public string ChatCompletionsUrl => BaseUrl + "/chat/completions";

    public string Model => NonEmpty("AiAgent:Model") ?? DefaultModel;

    /// <summary>Not needed for Ollama. Keep it out of the committed appsettings -- use user secrets or an
    /// environment variable (<c>AiAgent__ApiKey</c>) on the server.</summary>
    public string ApiKey => NonEmpty("AiAgent:ApiKey");

    /// <summary>
    /// Long by default because a CPU-only host can take well over a minute for one reply, and a timeout that
    /// fires first turns a slow answer into no answer.
    /// </summary>
    public int TimeoutSeconds =>
        int.TryParse(_appConfiguration["AiAgent:TimeoutSeconds"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : DefaultTimeoutSeconds;

    /// <summary>Low: answers about balances and payslips should be the same every time they are asked.</summary>
    public double Temperature =>
        double.TryParse(_appConfiguration["AiAgent:Temperature"], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value
            : DefaultTemperature;

    /// <summary>
    /// Qwen3 "thinks" before answering by default, which on a CPU host multiplies the wait several times over
    /// for no gain on lookups like these. On by default; set false for a model where the switch means nothing.
    /// </summary>
    public bool DisableThinking =>
        !bool.TryParse(_appConfiguration["AiAgent:DisableThinking"], out var value) || value;

    /// <summary>
    /// False unless explicitly set to <c>true</c>: the model may only be on this machine or the private network,
    /// so employee data never leaves it. See <see cref="AiModelHostGuard"/>. Turning this on is a data-protection
    /// decision, not a configuration detail.
    /// </summary>
    public bool AllowExternalModelHost =>
        bool.TryParse(_appConfiguration["AiAgent:AllowExternalModelHost"], out var value) && value;

    private string NonEmpty(string key)
    {
        var value = _appConfiguration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
