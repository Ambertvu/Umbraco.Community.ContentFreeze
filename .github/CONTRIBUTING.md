# Contributing Guidelines

Contributions to this package are most welcome! 

There is a test site in the solution to make working with this repository easier.
It is configured to do an unattended install, check `appSettings.json` for the login details.
## Branches

Each Umbraco major version has its own pair of branches, like Umbraco's own packages:

- `v17/dev` and `v18/dev`: open pull requests against the dev branch of the Umbraco version you are fixing.
- `v17/main` and `v18/main`: what has been released. A release is a tag such as `17.0.0` or `18.0.0` on the main branch.

A fix that applies to both versions goes into `v17/dev` first and is then merged into `v18/dev`.
