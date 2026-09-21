namespace HCMv3.Integrations;

/// <summary>
/// Names of mutually exclusive integration groups: sets in which at most one member may be enabled per
/// tenant at a time.
///
/// Exclusivity is enforced where enabling happens, not in the UI, because a card grid is only one of the
/// ways an integration can be switched on.
/// </summary>
public static class IntegrationExclusiveGroups
{
    /// <summary>
    /// The large-language-model provider behind the in-app assistant -- Azure OpenAI, Bedrock, a
    /// self-hosted Qwen, and so on.
    ///
    /// Exactly one may be live per tenant: the assistant sends each prompt to one endpoint, and two
    /// enabled providers would leave that choice to whichever the code happened to resolve first. Making
    /// them exclusive also means a tenant keeps every provider's saved credentials while only one is
    /// active, so switching back is a toggle rather than a re-entry of keys.
    /// </summary>
    public const string AiProvider = "AiProvider";
}
