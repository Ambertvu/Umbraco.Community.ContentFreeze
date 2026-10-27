# Content Freeze

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.ContentFreeze?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.ContentFreeze?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze)
[![GitHub license](https://img.shields.io/github/license/Ambertvu/Umbraco.Community.ContentFreeze?color=8AB803)](https://github.com/Ambertvu/Umbraco.Community.ContentFreeze/blob/main/LICENSE)

Safe deploys for Umbraco 17. Temporarily block content and media changes in the backoffice while you deploy, run migrations or import with uSync.

- Freeze from your CI/CD pipeline through an idempotent API, or by hand from the Settings dashboard.
- Editors get a countdown and warnings before the freeze starts; workspaces turn read-only while it runs.
- Every freeze has an end time and expires by itself.
- Enforced on the server, with permission-based bypass, an audit trail, webhooks and a health check.
- Pause background jobs during a freeze; hold back Hangfire jobs with the `Umbraco.Community.ContentFreeze.Hangfire` add-on.

See the [documentation on GitHub](https://github.com/Ambertvu/Umbraco.Community.ContentFreeze) for setup and Azure Pipelines and GitHub Actions examples.
