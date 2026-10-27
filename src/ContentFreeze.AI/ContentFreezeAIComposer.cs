using ContentFreeze.Composers;
using ContentFreeze.Enforcement;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tests;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace ContentFreeze.AI;

[ComposeAfter(typeof(ContentFreezeComposer))]
public class ContentFreezeAIComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<ContentFreezeAIOptions>().Bind(builder.Config.GetSection(ContentFreezeAIOptions.SectionName));

        builder.AddFreezeEndpointRule<AIFreezeRule>();
        builder.AddFrozenActions<AIFrozenActions>();

        builder
            .AddNotificationHandler<AIConnectionSavingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIConnectionDeletingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIConnectionRollingBackNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIProfileSavingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIProfileDeletingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIProfileRollingBackNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIContextSavingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIContextDeletingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIContextRollingBackNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIGuardrailSavingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIGuardrailDeletingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AIGuardrailRollingBackNotification, AIFreezeHandler>()
            .AddNotificationHandler<AISettingsSavingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AITestSavingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AITestDeletingNotification, AIFreezeHandler>()
            .AddNotificationHandler<AITestRollingBackNotification, AIFreezeHandler>();
    }
}
