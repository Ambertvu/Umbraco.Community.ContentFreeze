using ContentFreeze.Enforcement;
using ContentFreeze.Forms;
using ContentFreeze.Tests.Support;
using Microsoft.AspNetCore.Http;
using Moq;
using Umbraco.Cms.Core.Events;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Services.Notifications;

namespace ContentFreeze.Tests.Forms;

public class FormsFreezeTests
{
    private readonly TestFreezeService _test = new();
    private readonly ContentFreezeFormsOptions _options = new();

    private string? Operation<TController>(string method = "PUT")
        => new FormsFreezeRule(new StaticOptionsMonitor<ContentFreezeFormsOptions>(_options))
            .GetOperation(ActionContexts.Action<TController>(), ActionContexts.Request(method));

    [Fact]
    public void Form_changes_are_picked_out()
    {
        Assert.Equal("forms form change", Operation<Umbraco.Forms.Web.Api.ManagementApi.Form.UpdateFormController>());
        Assert.NotNull(Operation<Umbraco.Forms.Web.Api.ManagementApi.DataSource.Wizard.CreateFormFromDataSourceWizardController>("POST"));
    }

    [Fact]
    public void Posts_that_only_read_or_validate_are_left_alone()
    {
        Assert.Null(Operation<Umbraco.Forms.Web.Api.ManagementApi.Analytics.PostSubmissionsController>("POST"));
        Assert.Null(Operation<Umbraco.Forms.Web.Api.ManagementApi.Form.Field.ValidateFieldSettingsController>("POST"));
    }

    [Fact]
    public void Website_submissions_are_never_picked_out()
    {
        _options.FreezeEntries = true;

        Assert.Null(Operation<Umbraco.Forms.Web.Controllers.UmbracoFormsController>("POST"));
        Assert.Null(Operation<Umbraco.Forms.Web.Api.DeliveryApi.EntriesController>("POST"));
    }

    [Fact]
    public void Entries_and_permissions_follow_their_settings()
    {
        Assert.Null(Operation<Umbraco.Forms.Web.Api.ManagementApi.Record.ExecuteActionController>("POST"));
        Assert.Null(Operation<Umbraco.Forms.Web.Api.ManagementApi.Security.UpdateUserFormSecurityController>());

        _options.FreezeEntries = true;
        _options.FreezeSecurity = true;

        Assert.Equal("forms entry change", Operation<Umbraco.Forms.Web.Api.ManagementApi.Record.ExecuteActionController>("POST"));
        Assert.Equal("forms permission change", Operation<Umbraco.Forms.Web.Api.ManagementApi.Security.UpdateUserFormSecurityController>());
    }

    [Fact]
    public void FreezeForms_off_lets_form_changes_through()
    {
        _options.FreezeForms = false;

        Assert.Null(Operation<Umbraco.Forms.Web.Api.ManagementApi.Form.UpdateFormController>());
    }

    [Fact]
    public void Hidden_actions_follow_the_settings()
    {
        IEnumerable<string> Aliases() => new FormsFrozenActions(Microsoft.Extensions.Options.Options.Create(_options)).GetFrozenActionAliases();

        Assert.Contains("Forms.WorkspaceAction.Form.Save", Aliases());
        Assert.DoesNotContain("Forms.EntityBulkAction.Entry.Delete", Aliases());

        _options.FreezeForms = false;
        _options.FreezeEntries = true;

        Assert.DoesNotContain("Forms.WorkspaceAction.Form.Save", Aliases());
        Assert.Contains("Forms.EntityBulkAction.Entry.Delete", Aliases());
    }

    [Fact]
    public async Task Saving_a_form_from_code_is_cancelled_during_a_freeze()
    {
        await _test.ScheduleAsync();
        var messages = new EventMessages();
        var enforcer = new FreezeEnforcer(FreezeGuardFactory.Create(_test), Mock.Of<IEventMessagesFactory>(f => f.Get() == messages));
        var handler = new FormsFreezeHandler(enforcer, new StaticOptionsMonitor<ContentFreezeFormsOptions>(_options));

        var saving = new FormSavingNotification(new Form(), messages);
        handler.Handle(saving);

        Assert.True(saving.Cancel);

        _options.FreezeForms = false;
        var notBlocked = new WorkflowSavingNotification(new Workflow(), messages);
        handler.Handle(notBlocked);

        Assert.False(notBlocked.Cancel);
    }
}
