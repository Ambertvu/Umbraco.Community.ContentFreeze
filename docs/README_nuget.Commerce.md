# Content Freeze for Umbraco Commerce

[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.ContentFreeze.Commerce?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze.Commerce)

Add-on for [Umbraco.Community.ContentFreeze](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze). While a content freeze is active, Umbraco Commerce settings are frozen too, and product stock stays put.

- Stores, currencies, countries, taxes, payment and shipping methods, templates, order statuses, locations and product attributes cannot be changed in the backoffice.
- Fixes stock changing when a frozen product save is blocked: Commerce copies the stock value even when the save is cancelled.
- Orders, carts, checkout, payments and payment provider callbacks keep working.
- Optionally also freeze discounts, gift cards, customers and order edits.

See the [documentation on GitHub](https://github.com/Ambertvu/Umbraco.Community.ContentFreeze#umbraco-commerce) for settings.
