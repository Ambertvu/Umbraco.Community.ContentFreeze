using ContentFreeze.Enforcement;
using Microsoft.Extensions.Options;

namespace ContentFreeze.AI;

/// <summary>
/// The Umbraco AI backoffice actions to hide during a freeze. Aliases from packages that are not installed are ignored.
/// </summary>
/// <remarks>Rollback is a button inside the version history without an alias; only the server stops it.</remarks>
internal class AIFrozenActions : IFrozenActionsProvider
{
    private static readonly string[] CoreEntities = ["Connection", "Profile", "Context", "Guardrail"];

    private readonly IOptions<ContentFreezeAIOptions> _options;

    public AIFrozenActions(IOptions<ContentFreezeAIOptions> options) => _options = options;

    public IEnumerable<string> GetFrozenActionAliases()
    {
        ContentFreezeAIOptions options = _options.Value;
        var aliases = new List<string>();

        if (options.FreezeConfiguration)
        {
            aliases.AddRange(CoreEntities.SelectMany(entity => EntityAliases("UmbracoAI", entity)));
            aliases.Add("UmbracoAI.WorkspaceAction.Settings.Save");
            aliases.AddRange(EntityAliases("UmbracoAIPrompt", "Prompt"));
            aliases.AddRange(EntityAliases("UmbracoAIAgent", "Agent"));
        }

        if (options.FreezeTests)
        {
            aliases.AddRange(EntityAliases("UmbracoAI", "Test"));
        }

        // The copilot's tools to save or publish the document it works on: the document cannot be saved anyway.
        aliases.AddRange(["Uai.AgentFrontendTool.Save", "Uai.AgentFrontendTool.SaveAndPublish"]);
        return aliases;
    }

    private static IEnumerable<string> EntityAliases(string prefix, string entity) =>
    [
        $"{prefix}.WorkspaceAction.{entity}.Save",
        $"{prefix}.EntityAction.{entity}.Create",
        $"{prefix}.EntityAction.{entity}.Delete",
        $"{prefix}.EntityBulkAction.{entity}.Delete",
        $"{prefix}.CollectionAction.{entity}.Create",
    ];
}
