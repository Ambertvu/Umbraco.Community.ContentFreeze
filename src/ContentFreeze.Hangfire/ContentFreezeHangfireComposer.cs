using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace ContentFreeze.Hangfire;

public class ContentFreezeHangfireComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<ContentFreezeHangfireOptions>().Bind(builder.Config.GetSection(ContentFreezeHangfireOptions.SectionName));
        builder.Services.AddSingleton<FreezeJobFilter>();
        builder.AddNotificationHandler<UmbracoApplicationStartingNotification, RegisterFreezeJobFilter>();
    }
}

/// <summary>
/// Adds the filter to Hangfire's global filters, before the Hangfire server (a hosted service) starts.
/// A global filter works however Hangfire was set up, Cultiv.Hangfire included.
/// </summary>
internal class RegisterFreezeJobFilter : INotificationHandler<UmbracoApplicationStartingNotification>
{
    private static bool _registered;

    private readonly FreezeJobFilter _filter;

    public RegisterFreezeJobFilter(FreezeJobFilter filter) => _filter = filter;

    public void Handle(UmbracoApplicationStartingNotification notification)
    {
        if (_registered)
        {
            return;
        }

        GlobalJobFilters.Filters.Add(_filter);
        _registered = true;
    }
}
