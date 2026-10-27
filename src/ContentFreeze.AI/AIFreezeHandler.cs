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

    public void Handle(AIConnectionSavingNotification notification) => Configuration(notification, "ai connection save");

    public void Handle(AIConnectionDeletingNotification notification) => Configuration(notification, "ai connection delete");

    public void Handle(AIConnectionRollingBackNotification notification) => Configuration(notification, "ai connection rollback");

    public void Handle(AIProfileSavingNotification notification) => Configuration(notification, "ai profile save");

    public void Handle(AIProfileDeletingNotification notification) => Configuration(notification, "ai profile delete");

    public void Handle(AIProfileRollingBackNotification notification) => Configuration(notification, "ai profile rollback");

    public void Handle(AIContextSavingNotification notification) => Configuration(notification, "ai context save");

    public void Handle(AIContextDeletingNotification notification) => Configuration(notification, "ai context delete");

    public void Handle(AIContextRollingBackNotification notification) => Configuration(notification, "ai context rollback");

    public void Handle(AIGuardrailSavingNotification notification) => Configuration(notification, "ai guardrail save");

    public void Handle(AIGuardrailDeletingNotification notification) => Configuration(notification, "ai guardrail delete");

    public void Handle(AIGuardrailRollingBackNotification notification) => Configuration(notification, "ai guardrail rollback");

    public void Handle(AISettingsSavingNotification notification) => Configuration(notification, "ai settings save");

    public void Handle(AITestSavingNotification notification) => Test(notification, "ai test save");

    public void Handle(AITestDeletingNotification notification) => Test(notification, "ai test delete");

    public void Handle(AITestRollingBackNotification notification) => Test(notification, "ai test rollback");

    private void Configuration(ICancelableNotification notification, string operation)
    {
        if (_options.CurrentValue.FreezeConfiguration)
        {
            _enforcer.Block(notification, operation);
        }
    }

    private void Test(ICancelableNotification notification, string operation)
    {
        if (_options.CurrentValue.FreezeTests)
        {
            _enforcer.Block(notification, operation);
        }
    }
}
