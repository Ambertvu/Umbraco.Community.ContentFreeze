// Stand-ins for Umbraco Forms controllers (the tests do not reference Forms.Web), and for a Commerce controller outside
// the Management API: the freeze rules only look at the namespace and class name.

namespace Umbraco.Commerce.Cms.Web.Api.Storefront.Controllers
{
    internal class UpdateOrderManagementApiController;
}

namespace Umbraco.AI.Web.Api.Management.Connection.Controllers
{
    internal class UpdateConnectionController;

    internal class TestConnectionController;
}

namespace Umbraco.AI.Web.Api.Management.Settings.Controllers
{
    internal class UpdateSettingsController;
}

namespace Umbraco.AI.Web.Api.Management.Test.Controllers
{
    internal class UpdateTestController;

    internal class RunTestController;
}

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers
{
    internal class DeleteTestRunController;
}

namespace Umbraco.AI.Web.Api.Management.AuditLog.Controllers
{
    internal class DeleteAuditLogController;
}

namespace Umbraco.AI.Web.Api.Management.Chat.Controllers
{
    internal class CompleteChatController;
}

namespace Umbraco.AI.Web.Api.Management.Versioning.Controllers
{
    internal class EntityVersionHistoryController;
}

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers
{
    internal class CreatePromptController;

    internal class ExecutePromptController;
}

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers
{
    internal class DeleteAgentController;

    internal class StreamAgentController;
}


namespace Umbraco.Forms.Web.Api.ManagementApi.Form
{
    internal class UpdateFormController;
}

namespace Umbraco.Forms.Web.Api.ManagementApi.Form.Field
{
    internal class ValidateFieldSettingsController;
}

namespace Umbraco.Forms.Web.Api.ManagementApi.DataSource.Wizard
{
    internal class CreateFormFromDataSourceWizardController;
}

namespace Umbraco.Forms.Web.Api.ManagementApi.Analytics
{
    internal class PostSubmissionsController;
}

namespace Umbraco.Forms.Web.Api.ManagementApi.Record
{
    internal class ExecuteActionController;
}

namespace Umbraco.Forms.Web.Api.ManagementApi.Security
{
    internal class UpdateUserFormSecurityController;
}

namespace Umbraco.Forms.Web.Api.DeliveryApi
{
    internal class EntriesController;
}

namespace Umbraco.Forms.Web.Controllers
{
    internal class UmbracoFormsController;
}
