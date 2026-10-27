using ContentFreeze.Enforcement;
using ContentFreeze.Manifest;
using ContentFreeze.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using Umbraco.Cms.Api.Management.Controllers.Document;
using Umbraco.Cms.Api.Management.Controllers.DocumentBlueprint;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Notifications;

namespace ContentFreeze.Tests;

public class ExtensionPointTests
{
    private readonly TestFreezeService _test = new();

    private FreezeEndpointFilter CreateFilter(params IFreezeEndpointRule[] rules) => new(rules, FreezeGuardFactory.Create(_test));

    private static async Task<bool> RunsAction(FreezeEndpointFilter filter, ActionExecutingContext context)
    {
        var ran = false;
        await filter.OnActionExecutionAsync(context, () =>
        {
            ran = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });
        return ran;
    }

    [Fact]
    public async Task Writes_picked_out_by_a_rule_get_a_400_with_the_freeze_message()
    {
        await _test.ScheduleAsync();
        ActionExecutingContext context = ActionContexts.Executing<UpdateDocumentBlueprintController>("PUT");

        Assert.False(await RunsAction(CreateFilter(new BlueprintFreezeRule(new StaticOptionsMonitor<Configuration.ContentFreezeOptions>(_test.Options))), context));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("Editing is temporarily blocked (Release). Your change was not saved.", problem.Detail);
    }

    [Theory]
    [InlineData("GET", true)]
    [InlineData("POST", false)]
    [InlineData("DELETE", false)]
    public async Task Only_writes_are_checked(string method, bool runs)
    {
        await _test.ScheduleAsync();

        Assert.Equal(runs, await RunsAction(CreateFilter(new AlwaysRule()), ActionContexts.Executing<UpdateDocumentController>(method)));
    }

    [Fact]
    public async Task Nothing_is_blocked_without_an_active_freeze()
        => Assert.True(await RunsAction(CreateFilter(new AlwaysRule()), ActionContexts.Executing<UpdateDocumentController>("PUT")));

    [Fact]
    public async Task Requests_no_rule_picks_out_go_through()
    {
        await _test.ScheduleAsync();

        Assert.True(await RunsAction(CreateFilter(new NeverRule()), ActionContexts.Executing<UpdateDocumentController>("PUT")));
    }

    [Fact]
    public void Blueprint_rule_follows_the_FreezeBlueprints_setting()
    {
        var rule = new BlueprintFreezeRule(new StaticOptionsMonitor<Configuration.ContentFreezeOptions>(_test.Options));
        HttpRequest request = ActionContexts.Request("PUT");

        Assert.NotNull(rule.GetOperation(ActionContexts.Action<UpdateDocumentBlueprintController>(), request));
        Assert.Null(rule.GetOperation(ActionContexts.Action<UpdateDocumentController>(), request));

        _test.Options.FreezeBlueprints = false;
        Assert.Null(rule.GetOperation(ActionContexts.Action<UpdateDocumentBlueprintController>(), request));
    }

    [Fact]
    public async Task The_enforcer_cancels_the_notification_with_the_freeze_message()
    {
        await _test.ScheduleAsync();
        var messages = new EventMessages();
        var enforcer = new FreezeEnforcer(FreezeGuardFactory.Create(_test), Mock.Of<IEventMessagesFactory>(f => f.Get() == messages));
        var notification = new TestNotification();

        Assert.True(enforcer.Block(notification, "test"));
        Assert.True(notification.Cancel);
        Assert.Equal("Editing is temporarily blocked (Release). Your change was not saved.", Assert.Single(messages.GetAll()).Message);

        // Already cancelled: no second message.
        Assert.False(enforcer.Block(notification, "test"));
        Assert.Single(messages.GetAll());
    }

    [Fact]
    public void The_enforcer_leaves_notifications_alone_without_a_freeze()
    {
        var enforcer = new FreezeEnforcer(FreezeGuardFactory.Create(_test), Mock.Of<IEventMessagesFactory>(f => f.Get() == new EventMessages()));
        var notification = new TestNotification();

        Assert.False(enforcer.Block(notification, "test"));
        Assert.False(notification.Cancel);
    }

    [Fact]
    public async Task Frozen_action_aliases_reach_the_backoffice_as_one_manifest()
    {
        var reader = new FrozenActionsManifestReader([new StaticAliases("A", "B", "A"), new StaticAliases()]);

        PackageManifest manifest = Assert.Single(await reader.ReadPackageManifestsAsync());

        var extension = Assert.Single(manifest.Extensions);
        dynamic meta = extension.GetType().GetProperty("meta")!.GetValue(extension)!;
        Assert.Equal(["A", "B"], (string[])meta.GetType().GetProperty("aliases")!.GetValue(meta));
        Assert.Equal(FrozenActionsManifestReader.ExtensionType, extension.GetType().GetProperty("type")!.GetValue(extension));
    }

    [Fact]
    public async Task No_manifest_without_frozen_actions()
        => Assert.Empty(await new FrozenActionsManifestReader([new StaticAliases()]).ReadPackageManifestsAsync());

    private sealed class AlwaysRule : IFreezeEndpointRule
    {
        public string? GetOperation(ControllerActionDescriptor action, HttpRequest request) => "test";
    }

    private sealed class NeverRule : IFreezeEndpointRule
    {
        public string? GetOperation(ControllerActionDescriptor action, HttpRequest request) => null;
    }

    private sealed class StaticAliases(params string[] aliases) : IFrozenActionsProvider
    {
        public IEnumerable<string> GetFrozenActionAliases() => aliases;
    }

    private sealed class TestNotification : ICancelableNotification
    {
        public bool Cancel { get; set; }
    }
}
