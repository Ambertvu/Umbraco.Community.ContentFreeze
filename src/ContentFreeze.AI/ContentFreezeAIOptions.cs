namespace ContentFreeze.AI;

/// <summary>
/// Settings bound from the <c>ContentFreeze:AI</c> section in appsettings. Using AI (chat, prompts, agents, property
/// actions) is never blocked, whatever these say.
/// </summary>
public class ContentFreezeAIOptions
{
    public const string SectionName = "ContentFreeze:AI";

    /// <summary>Connections, profiles, contexts, guardrails, AI settings, prompts and agents, and rolling them back.</summary>
    public bool FreezeConfiguration { get; set; } = true;

    /// <summary>AI tests and their baseline runs. Running tests keeps working.</summary>
    public bool FreezeTests { get; set; }
}
