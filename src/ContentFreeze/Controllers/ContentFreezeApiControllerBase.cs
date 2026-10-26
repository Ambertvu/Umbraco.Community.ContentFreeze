using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace ContentFreeze.Controllers
{
    // Any backoffice or API user may read the status; changes require ContentFreezeAuthorizationPolicies.Manage.
    [ApiController]
    [BackOfficeRoute("contentfreeze/api/v{version:apiVersion}")]
    [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
    [MapToApi(Constants.ApiName)]
    [JsonOptionsName(Umbraco.Cms.Core.Constants.JsonOptionsNames.BackOffice)]
    public class ContentFreezeApiControllerBase : ControllerBase
    {
    }
}
