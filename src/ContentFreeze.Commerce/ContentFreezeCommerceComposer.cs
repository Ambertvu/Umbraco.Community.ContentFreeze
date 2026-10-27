using ContentFreeze.Composers;
using ContentFreeze.Enforcement;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Commerce.Core.Events.Validation;
using Umbraco.Commerce.Extensions;

namespace ContentFreeze.Commerce;

[ComposeAfter(typeof(ContentFreezeComposer))]
public class ContentFreezeCommerceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<ContentFreezeCommerceOptions>().Bind(builder.Config.GetSection(ContentFreezeCommerceOptions.SectionName));

        builder.AddFreezeEndpointRule<CommerceFreezeRule>();
        builder.AddFrozenActions<CommerceFrozenActions>();

        builder.WithUmbracoCommerceBuilder()
            .WithValidationEvent<ValidateStockChange>()
            .RegisterHandler<FreezeStockChangeHandler>();
    }
}
