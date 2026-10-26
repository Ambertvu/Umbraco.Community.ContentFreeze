# Content Freeze for Hangfire

[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.ContentFreeze.Hangfire?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze.Hangfire)

Add-on for [Umbraco.Community.ContentFreeze](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze). While a content freeze is active, Hangfire jobs (for example from Cultiv.Hangfire) are held back and run once the freeze ends.

- Works with any Hangfire setup through a global job filter; no code changes needed.
- Held-back jobs show as Scheduled in the Hangfire dashboard, with the freeze as reason.
- A recurring job keeps one held-back run, so a job that runs every minute fires once after the freeze, not sixty times.
- Hold back everything, or pick jobs by recurring job id or `Type.Method` in the `ContentFreeze:Hangfire` settings.

See the [documentation on GitHub](https://github.com/Ambertvu/Umbraco.Community.ContentFreeze#hangfire) for settings.
