# Content Freeze

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.ContentFreeze?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.ContentFreeze?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze)
[![GitHub license](https://img.shields.io/github/license/Ambertvu/Umbraco.Community.ContentFreeze?color=8AB803)](../LICENSE)

Safe deploys for Umbraco. Content Freeze temporarily blocks content and media changes in the backoffice while you deploy, run migrations or import with uSync:

- **Freeze from your pipeline.** Schedule a freeze through the API, deploy, and release it again, from Azure Pipelines, GitHub Actions or anything that can make an HTTP call.
- **Warn editors first.** A countdown in the top bar and toasts five minutes and one minute before the start, so nobody loses work.
- **Always ends.** Every freeze has an end time. A pipeline that dies halfway never locks editors out for good.
- **Enforced on the server.** Saves, publishes, moves, deletes and the rest are cancelled by notification handlers, not just hidden in the UI.
- **Audit trail and webhooks.** See who froze editing, why and for how long, and send Slack or Teams messages through Umbraco webhooks.

Content Freeze is for Umbraco 17. Umbraco 18 support follows once it has been tested there.

## Installation

Add the package to an existing Umbraco 17 website from NuGet:

`dotnet add package Umbraco.Community.ContentFreeze`

Start the site. The package creates its two database tables on startup and gives the Administrators group the *Manage freezes* permission.

## How a freeze works

A freeze is a time window, not a switch:

| Now | State | Effect |
|---|---|---|
| before the start | Scheduled | Editors see a countdown and warnings, nothing is blocked yet |
| between start and end | Active | Changes are blocked |
| after the end, or ended manually | Expired / Ended | Nothing |

Every server works out the state from the database and its clock, so load-balanced sites agree without extra coordination. Only one freeze can be scheduled or active at a time.

### What is blocked

While a freeze is active, these are cancelled for backoffice and API users:

- Content: save, publish, unpublish, send for approval, move, copy, sort, roll back, move to or restore from the recycle bin, delete, empty the recycle bin, delete versions.
- Media: save (including uploads), move, move to recycle bin, delete, empty the recycle bin, delete versions.
- Document blueprints, dictionary items, culture and hostnames and public access. Each of these can be switched off in settings.

The backoffice also makes document and media workspaces read-only and hides the write actions, so editors do not find out only when they press Save.

**Not blocked:** the website itself, members, settings (document types, data types, templates), and code that runs without a backoffice user, such as scheduled jobs, form handlers or imports. uSync imports go through by default; see [uSync](#usync). See [Enforcement modes](#enforcement-modes) if you want those blocked too, and [Background jobs](#background-jobs) to pause jobs during a freeze.

### Scheduled publishing

Scheduled publishing is held back while a freeze is active. Items that were due during the freeze are published (or unpublished) on the first run after it ends. Set `DeferScheduledPublishing` to `false` to let it run during a freeze.

### uSync

A deploy usually imports with uSync while editing is frozen, so uSync imports are let through by default: at startup, from the uSync dashboard and in both [enforcement modes](#enforcement-modes). Everything else stays blocked during the import, including editors saving at the same moment.

Set `AllowUSyncImports` to `false` to block uSync imports like any other change.

## Permissions

Two permissions appear in each user group under **Content freeze**:

| Permission | Allows |
|---|---|
| Manage freezes | Schedule, extend, end and cancel freezes, from the dashboard and the API |
| Edit during a freeze | Keep editing while a freeze is active |

On install, *Manage freezes* is given to Administrators. Nobody gets *Edit during a freeze* by default, not even administrators.

## Dashboard

**Settings → Content Freeze** (needs *Manage freezes*) shows the current freeze: who started it (a user or a pipeline's API user), its correlation id, reason and times. From there you can extend it, end it early, or cancel one that has not started. With no freeze running, you can start one by hand. The history keeps a log per freeze, including how many changes were blocked.

## Login screen

Editors who log in during a freeze, or shortly before one, see a notice above the login form: when editing will be frozen, or until when it is frozen. It switches from one to the other at the start time and disappears when the freeze ends, without reloading the page.

Anyone can open the login screen, so the notice shows only the times: not the reason, the correlation id or who started the freeze. Set `ShowOnLoginScreen` to `false` to leave it out.

## Using it from a pipeline

### 1. Create an API user

1. In **Users**, create a user group, for example *Deployments*, with the *Manage freezes* permission.
2. Create an **API user** in that group and add client credentials. Umbraco requires the client id to start with `umbraco-back-office-`, for example `umbraco-back-office-deployments`.

If your pipeline also changes content through the Management API (for example a content import), give the group *Edit during a freeze* too. Otherwise the pipeline blocks itself.

### 2. The API

All endpoints are under `/umbraco/contentfreeze/api/v1` and need a bearer token from `/umbraco/management/api/v1/security/back-office/token` (client credentials). Swagger: `/umbraco/swagger/?urls.primaryName=Content+Freeze+Backoffice+API`.

| Method | Path | Does |
|---|---|---|
| `POST` | `/freeze` | Schedule a freeze or start one now. `201` new, `200` existing (same correlation id), `409` another freeze is scheduled or active |
| `GET` | `/freeze/status` | The scheduled or active freeze, if any |
| `GET` | `/freeze/login-status` | Only the state and times, without authentication. Used by the login screen |
| `GET` | `/freeze/{id}` | A freeze by id, key or correlation id |
| `GET` | `/freeze/{id}/assert-active` | `200` when active, `409` otherwise. Use before a step that must not run while editors edit |
| `POST` | `/freeze/{id}/extend` | Push the end back: `{ "minutes": 15, "reason": "..." }` |
| `POST` | `/freeze/{id}/end` | End an active freeze or cancel a scheduled one. Always `200` for a freeze that has already ended or does not exist |
| `DELETE` | `/freeze/{id}` | Cancel a scheduled freeze; `409` when it is already active |

`{id}` can be the freeze id, its key or its correlation id.

```json
POST /umbraco/contentfreeze/api/v1/freeze
{
  "startsInMinutes": 15,
  "durationMinutes": 45,
  "reason": "Release 4.2",
  "correlationId": "build-20261026.3"
}
```

- **Prefer `startsInMinutes`** over `startsAt`: the server works out the start, so the build agent's clock does not matter.
- **Use a correlation id**, such as the build id. A retry with the same id returns the freeze that is already there. Once that freeze has ended, the same id creates a new one, so a re-run of the pipeline freezes again.
- **`end` never fails** on a freeze that already ended or never existed, so your cleanup step stays green when someone ended the freeze early from the dashboard.

### Azure Pipelines

Each stage runs on its own agent, so each stage that calls the API fetches its own token. The wait runs in an agentless job, so it does not occupy an agent.

```yaml
variables:
  FreezeApi: $(UmbracoUrl)/umbraco/contentfreeze/api/v1

stages:
- stage: AnnounceFreeze
  jobs:
  - job: Announce
    steps:
    - pwsh: |
        $token = (Invoke-RestMethod -Method Post -Uri "$(UmbracoUrl)/umbraco/management/api/v1/security/back-office/token" `
          -Body @{ grant_type = "client_credentials"; client_id = "$(ClientId)"; client_secret = "$(ClientSecret)" }).access_token
        Invoke-RestMethod -Method Post -Uri "$(FreezeApi)/freeze" -Headers @{ Authorization = "Bearer $token" } `
          -ContentType "application/json" `
          -Body (@{ startsInMinutes = 15; durationMinutes = 60; reason = "Release $(Build.BuildNumber)"; correlationId = "$(Build.BuildId)" } | ConvertTo-Json)

- stage: WaitForFreeze
  dependsOn: AnnounceFreeze
  jobs:
  - job: Wait
    pool: server
    steps:
    - task: Delay@1
      inputs:
        delayForMinutes: '15'

- stage: Deploy
  dependsOn: WaitForFreeze
  jobs:
  - job: Deploy
    steps:
    - pwsh: |
        $token = (Invoke-RestMethod -Method Post -Uri "$(UmbracoUrl)/umbraco/management/api/v1/security/back-office/token" `
          -Body @{ grant_type = "client_credentials"; client_id = "$(ClientId)"; client_secret = "$(ClientSecret)" }).access_token
        # Fails the stage (409) if the freeze is not active, for example because someone ended it early.
        Invoke-RestMethod -Uri "$(FreezeApi)/freeze/$(Build.BuildId)/assert-active" -Headers @{ Authorization = "Bearer $token" }
      displayName: Check the freeze is active
    # ... deploy, run migrations, import with uSync ...

- stage: Unfreeze
  dependsOn: Deploy
  condition: always()
  jobs:
  - job: Unfreeze
    steps:
    - pwsh: |
        $token = (Invoke-RestMethod -Method Post -Uri "$(UmbracoUrl)/umbraco/management/api/v1/security/back-office/token" `
          -Body @{ grant_type = "client_credentials"; client_id = "$(ClientId)"; client_secret = "$(ClientSecret)" }).access_token
        # Ends the freeze, or cancels it if the build failed before it started.
        Invoke-RestMethod -Method Post -Uri "$(FreezeApi)/freeze/$(Build.BuildId)/end" -Headers @{ Authorization = "Bearer $token" } `
          -ContentType "application/json" -Body '{ "reason": "Pipeline finished" }'
```

### GitHub Actions

```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    env:
      FREEZE_API: ${{ vars.UMBRACO_URL }}/umbraco/contentfreeze/api/v1
    steps:
      - name: Get Umbraco token
        run: |
          TOKEN=$(curl -sf -X POST "${{ vars.UMBRACO_URL }}/umbraco/management/api/v1/security/back-office/token" \
            -d grant_type=client_credentials -d client_id=${{ vars.UMBRACO_CLIENT_ID }} \
            -d client_secret=${{ secrets.UMBRACO_CLIENT_SECRET }} | jq -r .access_token)
          echo "::add-mask::$TOKEN"
          echo "TOKEN=$TOKEN" >> "$GITHUB_ENV"

      - name: Announce freeze
        run: |
          curl -sf -X POST "$FREEZE_API/freeze" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
            -d "{\"startsInMinutes\":10,\"durationMinutes\":45,\"reason\":\"Release ${{ github.ref_name }}\",\"correlationId\":\"${{ github.run_id }}-${{ github.run_attempt }}\"}"

      - name: Wait for the freeze to start
        timeout-minutes: 20
        run: |
          until curl -sf "$FREEZE_API/freeze/${{ github.run_id }}-${{ github.run_attempt }}/assert-active" -H "Authorization: Bearer $TOKEN" > /dev/null; do
            sleep 30
          done

      # ... deploy ...

      - name: End freeze
        if: always()
        run: |
          curl -sf -X POST "$FREEZE_API/freeze/${{ github.run_id }}-${{ github.run_attempt }}/end" \
            -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"reason":"Workflow finished"}'
```

Tokens expire. If your deploy takes longer than the token lifetime, fetch a new token before the last step.

## Allowing writes from code

Imports and background jobs that run inside a backoffice request (for example a dashboard button), or any code in Strict mode, can be let through explicitly:

```csharp
public class NightlyImport(IFreezeService freezeService, IContentService contentService)
{
    public void Run()
    {
        using (freezeService.AllowWrites("nightly-import"))
        {
            // Writes in this block, and in async code it calls, are not blocked.
        }
    }
}
```

## Background jobs

Background jobs keep running during a freeze; only scheduled publishing is held back by default. What happens to their changes depends on the [enforcement mode](#enforcement-modes): in `BackOfficeOnly` they go through, in `Strict` they are blocked and the job's save fails. To keep a job from running at all during a freeze, pause it.

### Pausing Umbraco background jobs

List the jobs in `PausedJobs`, by class name, full type name or (for distributed jobs) job name:

```json
"ContentFreeze": {
  "PausedJobs": [ "ContentVersionCleanupJob", "MyNightlyImportJob" ]
}
```

This works for Umbraco's own jobs and for jobs from packages or your site, both `IRecurringBackgroundJob` and `IDistributedBackgroundJob`. During a freeze, a paused job **skips** its runs; the next run follows the job's normal period. A job that runs every 10 minutes catches up within 10 minutes of the end, a daily job waits until the next day. The list is read at startup.

Some jobs cannot be paused, because the site needs them to keep servers in sync: `InstructionProcessJob`, `TouchServerJob` and the package's own `FreezeMaintenanceJob`.

Of Umbraco's own jobs, `ContentVersionCleanupJob` is the one worth pausing. `ScheduledPublishingJob` does not need to be listed; see [Scheduled publishing](#scheduled-publishing). Do not pause `WebhookFiring`: it also sends the freeze's own webhooks.

### Checking for a freeze in your own code

For your own jobs, imports or Hangfire jobs, ask `IFreezeService` and decide what to do yourself:

```csharp
public class MyNightlyImportJob(IFreezeService freezeService) : IRecurringBackgroundJob
{
    public async Task RunJobAsync()
    {
        if (freezeService.IsFrozen)
        {
            return; // try again next run
        }

        // ...
    }
}
```

`IsFrozen` is true while a freeze is active and not overridden by the `Disabled` setting, in both enforcement modes.

### Hangfire

Install the add-on to hold back Hangfire jobs, for example from [Cultiv.Hangfire](https://github.com/nul800sebastiaan/Cultiv.Hangfire), during a freeze:

`dotnet add package Umbraco.Community.ContentFreeze.Hangfire`

It adds a global Hangfire filter, so it works however Hangfire is set up. A job that is about to run during a freeze goes back to **Scheduled** and runs after the freeze; in the Hangfire dashboard you see it waiting, with the freeze as reason. Nothing is lost:

- Fire-and-forget and delayed jobs all run once the freeze ends.
- A recurring job keeps one waiting run. Its later occurrences during the freeze are deleted, so a job that runs every minute fires once after an hour-long freeze, not sixty times.
- Held-back jobs are checked again every few minutes (`RecheckMinutes`), and never wait past the freeze's end time. When a freeze ends early, they run within that interval.

```json
"ContentFreeze": {
  "Hangfire": {
    "PauseAllJobs": true,
    "Jobs": [],
    "ExceptJobs": [ "send-newsletter" ],
    "RecheckMinutes": 5
  }
}
```

| Setting | Default | Meaning |
|---|---|---|
| `PauseAllJobs` | `true` | Hold back every Hangfire job, except `ExceptJobs`. |
| `Jobs` | empty | When `PauseAllJobs` is `false`: the jobs to hold back. |
| `ExceptJobs` | empty | When `PauseAllJobs` is `true`: the jobs that keep running. |
| `RecheckMinutes` | `5` | How long a held-back job waits before it is checked again. |

Jobs are matched by recurring job id, `Type.Method` (`ImportService.RunAsync`), class name or full type name. Hangfire jobs are held back in both enforcement modes.

## Umbraco Forms

Install the add-on to freeze Umbraco Forms too:

`dotnet add package Umbraco.Community.ContentFreeze.Forms`

During a freeze, forms (with their workflows), folders, data sources and prevalue sources cannot be created, changed, moved or deleted in the backoffice; the save, create and delete actions are hidden. Submissions from the website are never blocked, in either enforcement mode.

```json
"ContentFreeze": {
  "Forms": {
    "FreezeForms": true,
    "FreezeEntries": false,
    "FreezeSecurity": false
  }
}
```

| Setting | Default | Meaning |
|---|---|---|
| `FreezeForms` | `true` | Forms, workflows, folders, data sources and prevalue sources. |
| `FreezeEntries` | `false` | Editing, approving, rejecting and deleting entries, and retrying workflows, in the backoffice. |
| `FreezeSecurity` | `false` | Form permissions per user and user group. |

Fields in the form designer stay editable during a freeze; the form just cannot be saved. Changes made from code are blocked too; uSync.Forms imports go through, like other [uSync](#usync) imports.

## Umbraco Commerce

Install the add-on when the site runs Umbraco Commerce:

`dotnet add package Umbraco.Community.ContentFreeze.Commerce`

Products are content, so the core package already freezes them. The add-on adds:

- **Settings:** stores, currencies, countries and regions, tax classes and calculation methods, payment and shipping methods, order statuses, locations, email, print and export templates, and product attributes and presets cannot be created, changed, sorted or deleted in the backoffice.
- **Stock:** without the add-on, a product save that the freeze blocks **still changes the product's stock**. Commerce copies the stock value to its own tables while the product is being saved, and does not check whether the save was cancelled. The add-on stops that, and also blocks stock changes through Commerce's own product API.

Orders, carts, checkout, payments and payment provider callbacks are never blocked, in either enforcement mode. Stock that an order books off still changes. Capturing, cancelling and refunding payments, and finalizing a cart, keep working in the backoffice too.

```json
"ContentFreeze": {
  "Commerce": {
    "FreezeSettings": true,
    "FreezeStock": true,
    "FreezeDiscounts": false,
    "FreezeGiftCards": false,
    "FreezeCustomers": false,
    "FreezeOrders": false
  }
}
```

| Setting | Default | Meaning |
|---|---|---|
| `FreezeSettings` | `true` | The store settings listed above. |
| `FreezeStock` | `true` | Stock edited in the backoffice. |
| `FreezeDiscounts` | `false` | Discounts. |
| `FreezeGiftCards` | `false` | Gift cards created or edited in the backoffice. Gift cards that orders create keep working. |
| `FreezeCustomers` | `false` | Customers. |
| `FreezeOrders` | `false` | Editing, deleting and changing the status of orders and carts in the backoffice. Off by default, so fulfilment can continue during a deploy. |

## Umbraco AI

Install the add-on to freeze the Umbraco AI configuration too:

`dotnet add package Umbraco.Community.ContentFreeze.AI`

During a freeze, connections, profiles, contexts, guardrails, the AI settings, prompts and agents cannot be created, changed, deleted or rolled back. Using AI is never blocked: chat, running prompts and agents, property actions, embeddings, image generation and testing a connection keep working.

Content that AI changes (for example through the copilot or a prompt applied to a property) is saved like any other change, so the core package already blocks it.

```json
"ContentFreeze": {
  "AI": {
    "FreezeConfiguration": true,
    "FreezeTests": false
  }
}
```

| Setting | Default | Meaning |
|---|---|---|
| `FreezeConfiguration` | `true` | Connections, profiles, contexts, guardrails, AI settings, prompts and agents. |
| `FreezeTests` | `false` | AI tests and their baseline runs. Running tests keeps working. |

## Freezing other packages

Content Freeze has three extension points for packages that keep their own data. Register them in a composer that runs after `ContentFreezeComposer`:

- **`IFreezeEndpointRule`**: picks out Management API actions that change something. Matching requests get a 400 with the freeze message, before the controller runs. Use this when a package has no cancelable notifications, or handles a cancelled one badly.
- **`IFreezeEnforcer`**: cancels a notification with the freeze message, for your own notification handlers.
- **`IFrozenActionsProvider`**: backoffice action aliases to hide during a freeze, from C#, without a client build.

```csharp
[ComposeAfter(typeof(ContentFreezeComposer))]
public class MyPackageFreezeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder
        .AddFreezeEndpointRule<MyPackageFreezeRule>()
        .AddFrozenActions<MyPackageFrozenActions>();
}

public class MyPackageFreezeRule : IFreezeEndpointRule
{
    public string? GetOperation(ControllerActionDescriptor action, HttpRequest request)
        => action.ControllerTypeInfo.Namespace?.StartsWith("MyPackage.Api.Management", StringComparison.Ordinal) is true
            ? "my package change"
            : null;
}
```

Only write requests (POST, PUT, PATCH, DELETE) reach a rule. The Forms add-on is a complete example.

## Webhooks and notifications

In **Settings → Webhooks** you will find five events under *Content Freeze*: Scheduled, Started, Extended, Ended (manually or by expiry) and Cancelled. The payload has the freeze id, state, times, reason, correlation id, who started and ended it, and whether it ended early.

The same moments are published as Umbraco notifications (`FreezeScheduledNotification`, `FreezeStartedNotification`, `FreezeExtendedNotification`, `FreezeEndedNotification`, `FreezeCancelledNotification`) for your own handlers.

There is also a health check, **Content Freeze**, in the Health Check dashboard.

## Settings

```json
"ContentFreeze": {
  "Disabled": false,
  "Mode": "BackOfficeOnly",
  "DefaultDurationMinutes": 60,
  "MaxDurationMinutes": 480,
  "MinimumWarningMinutes": 0,
  "DeferScheduledPublishing": true,
  "AllowUSyncImports": true,
  "ShowOnLoginScreen": true,
  "PausedJobs": [],
  "FreezeDictionary": true,
  "FreezeDomains": true,
  "FreezePublicAccess": true,
  "FreezeBlueprints": true
}
```

| Setting | Default | Meaning |
|---|---|---|
| `Disabled` | `false` | Break-glass switch: freezes are still recorded and shown, but nothing is blocked. For when you have locked yourself out. |
| `Mode` | `BackOfficeOnly` | See below. |
| `DefaultDurationMinutes` | `60` | Duration when a request does not specify one. |
| `MaxDurationMinutes` | `480` | Maximum total length of a freeze, extensions included. |
| `MinimumWarningMinutes` | `0` | Minimum time between scheduling and start. Requests can skip it with `"force": true`. |
| `DeferScheduledPublishing` | `true` | Hold back scheduled publishing during a freeze. |
| `AllowUSyncImports` | `true` | Let uSync imports through during a freeze. See [uSync](#usync). |
| `ShowOnLoginScreen` | `true` | Show a scheduled or active freeze on the login screen. See [Login screen](#login-screen). |
| `PausedJobs` | empty | Background jobs that skip their runs during a freeze. See [Background jobs](#background-jobs). |
| `FreezeDictionary`, `FreezeDomains`, `FreezePublicAccess`, `FreezeBlueprints` | `true` | Include these in the freeze. |

### Enforcement modes

- **`BackOfficeOnly`** (default): only changes made by a backoffice user or API user are blocked. Background jobs, startup imports and website code keep working.
- **`Strict`**: every change is blocked, unless it runs inside `AllowWrites(...)`, comes from a uSync import or the user has *Edit during a freeze*. This includes Umbraco's own jobs, such as version cleanup: they fail during the freeze and try again on their next run. [Pause them](#pausing-umbraco-background-jobs) to skip them cleanly instead.

The mode can be changed in appsettings without a restart.

## Known limitations

- **Error messages for bulk actions and publishing.** A blocked save shows the freeze message. When publishing, or using bulk actions in a list view, Umbraco shows its own generic error next to it.
- **Load balancing.** Changes to a freeze reach the other servers' caches through Umbraco's database messenger (5 seconds by default). Enforcement reads the database directly, so blocking is never late; only the banner can be.
- **Login screen notice.** It appears when the login screen opens during or before a freeze; a freeze scheduled while the login screen is already open shows after a reload. It is not shown on the two-factor and password reset screens.
- **Webhooks** fire on the scheduling server (or a single server), as with all Umbraco webhooks.
- **One freeze at a time.** Recurring or overlapping freeze windows are not supported yet.

## Contributing

Contributions to this package are most welcome! Please read the [Contributing Guidelines](CONTRIBUTING.md).

Run the tests with `dotnet test src/ContentFreeze.slnx`. `src/ContentFreeze.TestSite` is a site with the package, its add-ons, uSync, Umbraco Forms, Umbraco AI and Cultiv.Hangfire installed, for trying changes by hand. Build it with `-p:WithCommerce=true` and point it at SQL Server to test the Commerce add-on; on SQLite, Commerce locks the database while saving a product with stock.

## Acknowledgments

Built on Lotte Pitcher's [Opinionated Package Starter](https://github.com/LottePitcher/opinionated-package-starter).
