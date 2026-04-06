---
name: dotnet-wiki-maintainer
description: Use when the user asks to analyze a .NET repository area, architecture, flow, module, API, MassTransit consumer, saga, background job, tests, docs, branch diff, or implementation plan and then create, refresh, or improve GitHub Wiki pages in the wiki submodule.
---

# Dotnet Wiki Maintainer

Use this workflow to maintain the repository GitHub Wiki from source-code analysis.

## Goal

Analyze the requested .NET application area and create or update focused Markdown wiki pages inside the `wiki/` Git submodule. The output should help developers understand architecture, flows, operational behavior, and troubleshooting without reading every source file.

## Mandatory Workflow

1. Confirm the wiki submodule exists:
   - Check for `wiki/`.
   - Run `git -C wiki status`.
   - Run `git -C wiki rev-parse --show-toplevel`.
   - If the path is missing, stop and report that the wiki submodule path is missing.
   - If `--show-toplevel` resolves to the parent repository instead of `wiki/`, stop and report that the wiki submodule is not initialized.
2. Understand the user request:
   - Determine the target area: service, module, flow, API, consumer, saga, integration, deployment, test strategy, branch diff, or whole solution.
   - If the user asks for the whole solution, create or refresh overview pages instead of producing one huge page.
3. Inspect existing documentation:
   - Read `wiki/Home.md` if present.
   - Read `wiki/_Sidebar.md` if present.
   - Read related pages under `wiki/`.
   - Read root `README.md` and relevant files under `docs/` if present.
4. Inspect source code:
   - Find relevant projects in `*.sln` or `*.slnx`.
   - Inspect source files, tests, configuration, dependency injection, and deployment files.
   - For MassTransit or event-driven flows, inspect commands, events, consumers, sagas, state machines, activities, retry/dead-letter behavior, and message contracts.
   - For APIs, inspect endpoints/controllers, request/response models, validators, authorization, and integration clients.
   - For persistence, inspect DbContexts, repositories, migrations, entity configurations, and projections.
   - For operations, inspect logging, OpenTelemetry, health checks, retries, appsettings, pipeline, and deployment files.
5. Create an analysis summary before editing:
   - What the area does.
   - Which source files are most important.
   - Which wiki pages should be created or updated.
   - Any uncertainty.
6. Update wiki pages:
   - Prefer updating existing relevant pages over creating duplicates.
   - Create new pages only when the topic deserves its own page.
   - Keep pages focused and developer-oriented.
   - Use Mermaid diagrams when they clarify flows.
   - Include `Source map` sections with relative source paths.
   - Add `Open questions` when behavior is unclear.
   - Avoid documenting guesses as facts.
   - Do not include secrets or sensitive production values.
7. Update navigation:
   - Update `Home.md` when adding important new pages.
   - Update `_Sidebar.md` when adding, renaming, or restructuring pages.
8. Validate:
   - Run `git -C wiki diff`.
   - Check that links are reasonable.
   - Check Markdown headings and page titles.
   - Run `git status` from the parent repository.
   - Do not commit or push unless explicitly asked.

## Page Structure

For substantial pages, use:

```md
# Page Title
## Purpose
## When this runs
## Main flow
## Key components
## Configuration
## Failure modes and troubleshooting
## Source map
## Open questions
```

For overview pages, use:

```md
# Area Overview
## Purpose
## High-level architecture
## Main flows
## Important modules
## External dependencies
## Operational notes
## Related pages
## Source map
```

Load `references/wiki-page-template.md` only when a full page scaffold would help.

## Writing Style

- Write in clear technical English.
- Prefer concise sections over long prose.
- Use bullets and numbered flows where useful.
- Explain domain behavior before implementation details.
- Avoid marketing language.
- Avoid "AI-generated" phrasing.
- Clearly separate implemented behavior from open questions.

## Final Response

After editing, respond with:

1. Pages created.
2. Pages updated.
3. Key source files analyzed.
4. Important assumptions or uncertainties.
5. Suggested next wiki improvements.
6. Git status reminder for the wiki submodule and parent repository.
