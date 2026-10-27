using ContentFreeze.AI;
using ContentFreeze.Enforcement;
using ContentFreeze.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Tests;
using Umbraco.Cms.Core.Events;
using Connection = Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Management = Umbraco.AI.Web.Api.Management;

namespace ContentFreeze.Tests.AI;

public class AIFreezeTests
{
    private readonly TestFreezeService _test = new();
    private readonly ContentFreezeAIOptions _options = new();

    private string? Operation<TController>(string method = "PUT", string? entityType = null)
    {
        HttpRequest request = ActionContexts.Request(method);
        if (entityType is not null)
        {
            request.RouteValues["entityType"] = entityType;
        }

        return new AIFreezeRule(new StaticOptionsMonitor<ContentFreezeAIOptions>(_options))
            .GetOperation(ActionContexts.Action<TController>(), request);
    }

    [Fact]
    public void Configuration_changes_are_picked_out()
    {
        Assert.Equal("ai connection change", Operation<Connection.UpdateConnectionController>());
        Assert.Equal("ai settings change", Operation<Management.Settings.Controllers.UpdateSettingsController>());
        Assert.Equal("ai prompt change", Operation<Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers.CreatePromptController>("POST"));
        Assert.Equal("ai agent change", Operation<Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers.DeleteAgentController>("DELETE"));
        Assert.Equal("ai rollback", Operation<Management.Versioning.Controllers.EntityVersionHistoryController>("POST", "profile"));
    }

    [Fact]
    public void Using_AI_is_left_alone()
    {
        Assert.Null(Operation<Management.Chat.Controllers.CompleteChatController>("POST"));
        Assert.Null(Operation<Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers.ExecutePromptController>("POST"));
        Assert.Null(Operation<Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers.StreamAgentController>("POST"));
        Assert.Null(Operation<Connection.TestConnectionController>("POST"));
        Assert.Null(Operation<Management.Test.Controllers.RunTestController>("POST"));
        Assert.Null(Operation<Management.TestRun.Controllers.DeleteTestRunController>("DELETE"));
        Assert.Null(Operation<Management.AuditLog.Controllers.DeleteAuditLogController>("DELETE"));
    }

    [Fact]
    public void Tests_follow_their_own_setting()
    {
        Assert.Null(Operation<Management.Test.Controllers.UpdateTestController>());
        Assert.Null(Operation<Management.Versioning.Controllers.EntityVersionHistoryController>("POST", "test"));

        _options.FreezeTests = true;

        Assert.Equal("ai test change", Operation<Management.Test.Controllers.UpdateTestController>());
        Assert.Equal("ai rollback", Operation<Management.Versioning.Controllers.EntityVersionHistoryController>("POST", "test"));
    }

    [Fact]
    public void FreezeConfiguration_off_lets_configuration_changes_through()
    {
        _options.FreezeConfiguration = false;

        Assert.Null(Operation<Connection.UpdateConnectionController>());
        Assert.Null(Operation<Management.Versioning.Controllers.EntityVersionHistoryController>("POST", "profile"));
    }

    [Fact]
    public void Hidden_actions_follow_the_settings()
    {
        IEnumerable<string> Aliases() => new AIFrozenActions(Options.Create(_options)).GetFrozenActionAliases();

        Assert.Contains("UmbracoAI.WorkspaceAction.Connection.Save", Aliases());
        Assert.Contains("UmbracoAIPrompt.WorkspaceAction.Prompt.Save", Aliases());
        Assert.Contains("UmbracoAIAgent.EntityAction.Agent.Delete", Aliases());
        Assert.DoesNotContain("UmbracoAI.WorkspaceAction.Test.Save", Aliases());

        _options.FreezeTests = true;
        Assert.Contains("UmbracoAI.WorkspaceAction.Test.Save", Aliases());
    }

    [Fact]
    public async Task Configuration_saved_from_code_is_cancelled_during_a_freeze()
    {
        await _test.ScheduleAsync();
        var messages = new EventMessages();
        var enforcer = new FreezeEnforcer(FreezeGuardFactory.Create(_test), Mock.Of<IEventMessagesFactory>(f => f.Get() == messages));
        var handler = new AIFreezeHandler(enforcer, new StaticOptionsMonitor<ContentFreezeAIOptions>(_options));

        var connection = new AIConnectionDeletingNotification(Guid.NewGuid(), messages);
        var test = new AITestDeletingNotification(Guid.NewGuid(), messages);
        handler.Handle(connection);
        handler.Handle(test);

        Assert.True(connection.Cancel);
        Assert.False(test.Cancel);
    }
}
