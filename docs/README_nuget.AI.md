# Content Freeze for Umbraco AI

[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.ContentFreeze.AI?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze.AI)

Add-on for [Umbraco.Community.ContentFreeze](https://www.nuget.org/packages/Umbraco.Community.ContentFreeze). While a content freeze is active, the Umbraco AI configuration is frozen too: connections, profiles, contexts, guardrails, AI settings, prompts and agents.

- Using AI keeps working: chat, running prompts and agents, property actions, embeddings and image generation.
- Save, create and delete actions are hidden during a freeze, and blocked on the server.
- Content that AI changes is already frozen by the core package.

Requires Umbraco AI 18.

See the [documentation on GitHub](https://github.com/Ambertvu/Umbraco.Community.ContentFreeze#umbraco-ai) for settings.
