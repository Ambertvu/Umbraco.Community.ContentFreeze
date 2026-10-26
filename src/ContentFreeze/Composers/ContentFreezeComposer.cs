using ContentFreeze.Authorization;
using ContentFreeze.BackgroundJobs;
using ContentFreeze.Configuration;
using ContentFreeze.Enforcement;
using ContentFreeze.Migrations;
using ContentFreeze.Persistence;
using ContentFreeze.Services;
using ContentFreeze.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace ContentFreeze.Composers;

public class ContentFreezeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<ContentFreezeOptions>().Bind(builder.Config.GetSection(ContentFreezeOptions.SectionName));

        builder.Services.AddSingleton<IFreezeRepository, FreezeRepository>();
        builder.Services.AddSingleton<FreezeService>();
        builder.Services.AddSingleton<IFreezeService>(services => services.GetRequiredService<FreezeService>());
        builder.Services.AddSingleton<IFreezeGuard, FreezeGuard>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, RunContentFreezeMigrations>();
        builder.AddFreezeEnforcement();
        builder.Services.AddRecurringBackgroundJob<FreezeMaintenanceJob>();
        builder.AddPausableJobs();

        builder.Services.AddScoped<BlueprintFreezeFilter>();
        builder.Services.Configure<MvcOptions>(options => options.Filters.AddService<BlueprintFreezeFilter>());

        builder.Services.AddSingleton<IAuthorizationHandler, ManageFreezeAuthorizationHandler>();
        builder.Services.AddAuthorization(options => options.AddPolicy(ContentFreezeAuthorizationPolicies.Manage, policy =>
        {
            policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.Requirements.Add(new ManageFreezeRequirement());
        }));

        builder.WebhookEvents()
            .Add<FreezeScheduledWebhookEvent>()
            .Add<FreezeStartedWebhookEvent>()
            .Add<FreezeExtendedWebhookEvent>()
            .Add<FreezeEndedWebhookEvent>()
            .Add<FreezeCancelledWebhookEvent>();
    }
}
