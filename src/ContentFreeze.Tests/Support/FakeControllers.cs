// Stand-ins for Umbraco Forms controllers (the tests do not reference Forms.Web), and for a Commerce controller outside
// the Management API: the freeze rules only look at the namespace and class name.

namespace Umbraco.Commerce.Cms.Web.Api.Storefront.Controllers
{
    internal class UpdateOrderManagementApiController;
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
