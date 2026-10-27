using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace ContentFreeze.Composers
{
    public class ContentFreezeApiComposer : IComposer
    {
        // Related documentation:
        // https://docs.umbraco.com/umbraco-cms/tutorials/creating-a-backoffice-api
        // https://docs.umbraco.com/umbraco-cms/tutorials/creating-a-backoffice-api/umbraco-schema-and-operation-ids
        public void Compose(IUmbracoBuilder builder) =>
            builder.AddBackOfficeOpenApiDocument(
                Constants.ApiName,
                document => document
                    .WithTitle("Content Freeze Backoffice API")
                    .WithBackOfficeAuthentication()
                    .WithJsonOptions(Umbraco.Cms.Core.Constants.JsonOptionsNames.BackOffice)
                    .ConfigureOpenApiOptions(options =>
                    {
                        options.AddDocumentTransformer((doc, _, _) =>
                        {
                            doc.Info.Version = "1.0";
                            return Task.CompletedTask;
                        });

                        // Use the action name as operation ID, so the generated TypeScript client
                        // has short method names (FreezeService.status, FreezeService.schedule).
                        options.AddOperationTransformer((operation, context, _) =>
                        {
                            if (context.Description.ActionDescriptor.RouteValues.TryGetValue("action", out var action)
                                && action is not null)
                            {
                                operation.OperationId = action;
                            }

                            return Task.CompletedTask;
                        });
                    }));
    }
}
