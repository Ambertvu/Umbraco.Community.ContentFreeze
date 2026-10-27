using ContentFreeze.Composers;
using ContentFreeze.Enforcement;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Forms.Core.Services.Notifications;
using Umbraco.Extensions;

namespace ContentFreeze.Forms;

[ComposeAfter(typeof(ContentFreezeComposer))]
public class ContentFreezeFormsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<ContentFreezeFormsOptions>().Bind(builder.Config.GetSection(ContentFreezeFormsOptions.SectionName));

        builder.AddFreezeEndpointRule<FormsFreezeRule>();
        builder.AddFrozenActions<FormsFrozenActions>();

        builder
            .AddNotificationHandler<FormSavingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<FormDeletingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<WorkflowSavingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<WorkflowDeletingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<FolderSavingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<FolderDeletingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<DataSourceSavingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<DataSourceDeletingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<PrevalueSourceSavingNotification, FormsFreezeHandler>()
            .AddNotificationHandler<PrevalueSourceDeletingNotification, FormsFreezeHandler>();
    }
}
