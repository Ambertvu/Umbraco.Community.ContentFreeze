using ContentFreeze.Enforcement;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tests;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace ContentFreeze.AI;

/// <summary>
/// Backstop for Umbraco AI configuration changed outside the Management API, such as code or imports. Backoffice
/// requests are stopped earlier by <see cref="AIFreezeRule"/>. Prompts and agents live in separate packages and are
/// covered by the rule only.
/// </summary>
internal class AIFreezeHandler :
    INotificationHandler<AIConnectionSavingNotification>,
    INotificationHandler<AIConnectionDeletingNotification>,
    INotificationHandler<AIConnectionRollingBackNotification>,
    INotificationHandler<AIProfileSavingNotification>,
    INotificationHandler<AIProfileDeletingNotification>,
    INotificationHandler<AIProfileRollingBackNotification>,
    INotificationHandler<AIContextSavingNotification>,
    INotificationHandler<AIContextDeletingNotification>,
    INotificationHandler<AIContextRollingBackNotification>,
    INotificationHandler<AIGuardrailSavingNotification>,
    INotificationHandler<AIGuardrailDeletingNotification>,
    INotificationHandler<AIGuardrailRollingBackNotification>,
    INotificationHandler<AISettingsSavingNotification>,
    INotificationHandler<AITestSavingNotification>,
    INotificationHandler<AITestDeletingNotification>,
    INotificationHandler<AITestRollingBackNotification>
{
    private readonly IFreezeEnforcer _enforcer;
    private readonly IOptionsMonitor<ContentFreezeAIOptions> _options;

    public AIFreezeHandler(IFreezeEnforcer enforcer, IOptionsMonitor<ContentFreezeAIOptions> options)
    {
        _enforcer = enforcer;
        _options = options;
    }

    public void Handle(AIConnectionSavingNotification notification) => Block(notification, "ai connection save", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIConnectionDeletingNotification notification) => Block(notification, "ai connection delete", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIConnectionRollingBackNotification notification) => Block(notification, "ai connection rollback", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIProfileSavingNotification notification) => Block(notification, "ai profile save", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIProfileDeletingNotification notification) => Block(notification, "ai profile delete", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIProfileRollingBackNotification notification) => Block(notification, "ai profile rollback", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIContextSavingNotification notification) => Block(notification, "ai context save", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIContextDeletingNotification notification) => Block(notification, "ai context delete", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIContextRollingBackNotification notification) => Block(notification, "ai context rollback", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIGuardrailSavingNotification notification) => Block(notification, "ai guardrail save", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIGuardrailDeletingNotification notification) => Block(notification, "ai guardrail delete", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AIGuardrailRollingBackNotification notification) => Block(notification, "ai guardrail rollback", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AISettingsSavingNotification notification) => Block(notification, "ai settings save", _options.CurrentValue.FreezeConfiguration);

    public void Handle(AITestSavingNotification notification) => Block(notification, "ai test save", _options.CurrentValue.FreezeTests);

    public void Handle(AITestDeletingNotification notification) => Block(notification, "ai test delete", _options.CurrentValue.FreezeTests);

    public void Handle(AITestRollingBackNotification notification) => Block(notification, "ai test rollback", _options.CurrentValue.FreezeTests);

    private void Block(ICancelableNotification notification, string operation, bool frozen)
    {
        if (frozen)
        {
            _enforcer.Block(notification, operation);
        }
    }
}
