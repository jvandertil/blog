# Copilot Instructions

This repository contains the source for [jvandertil.nl](https://www.jvandertil.nl), a personal blog. It has three main parts:

- `src/blog` – Hugo static site (content, layouts, assets)
- `src/blog-comment-function` – Azure Functions v4 (.NET 9) that handle blog comment submissions
- `eng/_pipeline` – NUKE-based build pipeline (C#), including a custom Roslyn source generator

## Build & test commands

The build system is [NUKE](https://nuke.build/). All targets go through `build.ps1` (Windows) or `build.sh` (Linux/macOS).

```powershell
# Build everything (Hugo + Azure Function)
.\build.ps1

# Clean then build
.\build.ps1 --target Clean Build

# Serve the Hugo site locally (dev environment, includes drafts/future posts)
.\build.ps1 --target Serve
```

Hugo is downloaded automatically by the pipeline from GitHub Releases; the pinned version is defined in `eng/_pipeline/pipeline/IBlogContentPipeline.cs`.

### Running tests (Azure Function)

```powershell
# Via NUKE (runs as part of the standard build target)
.\build.ps1 --target Build

# Directly with dotnet
dotnet test src/blog-comment-function/tests/BlogComments.Tests/BlogComments.Tests.csproj

# Single test
dotnet test src/blog-comment-function/tests/BlogComments.Tests/BlogComments.Tests.csproj --filter "FullyQualifiedName~<TestName>"
```

Test framework: **xunit** + **FluentAssertions**. Test results (`.trx`) are written to `artifacts/TestResults/`.

## Architecture

### Comment flow

When a reader submits a comment, the Azure Function (`SubmitPostComment` / `SubmitPostCommentReply`):

1. Validates the post exists by checking the GitHub repository via the GitHub API (`GitHubPostExistenceValidator`).
2. Binds and validates the posted form using **FluentValidation** (`ModelBinder`).
3. Serializes the comment as JSON and commits it to a new branch in this repository under `src/blog/data/comments/posts/<postName>/<ulid>.json`.
4. Opens a pull request for the branch (when `GitHub:EnablePullRequestCreation` is `true`).

Comment threads support nested replies; the thread root and replies are stored in a single JSON file keyed by a ULID.

### GitHub App authentication

The function authenticates to GitHub as a GitHub App. `AppClientTokenGenerator` mints a short-lived JWT (RS256) signed by `ICryptographicSigner`. In production the key lives in Azure Key Vault (`KeyVaultRs256CryptographicSigner`); tests use `InMemoryRs256CryptographicSigner`.

### Strongly-typed Bicep source generator

`eng/_pipeline/StronglyTypedBicepGenerator` is a Roslyn `IIncrementalGenerator`. Applying `[BicepFile("path/to/file.bicep")]` to a partial class in the pipeline project generates strongly-typed `Deployments.*` and `Parameters.*` classes at compile time. See `eng/_pipeline/pipeline/Bicep.cs` for usage.

### Hugo environments

Configs live under `src/blog/config/`. Active environments: `dev`, `tst`, `prd`. The pipeline builds all three environments during `Build`; `Serve` uses `dev`.

## C# conventions

- **Private fields**: `_camelCase` prefix (e.g., `_repository`)
- **Constants / `static readonly` fields**: PascalCase (e.g., `CommentDataBasepath`)
- **Braces**: always required (`csharp_prefer_braces = true`)
- **Nullability**: `<Nullable>enable</Nullable>` in all projects; prefer null-conditional and null-coalescing operators
- **System directives first**: `dotnet_sort_system_directives_first = true`
- Use language keywords over framework types (`int` not `Int32`)
- JSON serialization uses `System.Text.Json` with `JsonNamingPolicy.CamelCase` + `WriteIndented = true` for comment data files

## Writing blog posts

### Working from raw notes

Draft posts are often raw notes — shell commands, config snippets, brief labels — not prose. When asked to help write or polish a post, treat these notes as the factual skeleton and shape them into a finished post that matches the style below.

### Post structure

1. **Opening (no heading)** — 1–3 short paragraphs stating the problem and why it matters. End with a sentence that tells the reader what the post will show them.
2. **Body sections (`##` headings)** — Walk through the solution step by step. Each section introduces what is about to happen in one or two sentences, then shows the code/config. Avoid over-explaining code that speaks for itself.
3. **Closing** — A short paragraph (or a few sentences) that wraps up. Can point out caveats, next steps, or just confirm "that's all you need."

Use `{{< notice >}}` … `{{< /notice >}}` for important warnings or gotchas.

### Voice and tone

- Write as a practitioner talking to a peer, not a teacher addressing a student.
- First person is fine ("I came up with…", "I generally prefer…", "I hope this is useful").
- Be direct and concise. Prefer short sentences. Cut filler phrases ("It is worth noting that…", "As you can see…").
- The target reader is a working .NET developer — including the author himself. Some posts are deep technical content; others are a "I solved this before, how?" personal reference that happens to be public. Both are valid goals.
- Assume the reader knows .NET and the relevant technology; explain the *specific technique*, not the basics.
- Explain the *why* behind a solution, not just the steps. The author's strength is understanding root causes; let that show.
- Italicised update notes at the end of the post are the convention for corrections added after publication (e.g. `*Update 18-07-2023: …*`).

### Scope

A focused post that ships is better than a comprehensive one that doesn't. When working from raw notes or a draft:
- Keep the post to one technique or one problem solved.
- If the material is trying to cover too many things, suggest splitting into separate posts rather than expanding the current one.
- Resist adding sections like "background", "alternatives", or "further reading" unless the draft explicitly contains that material.

### Front matter format

```toml
+++
title = "Post title here"
date  = 2026-01-01T12:00:00+01:00
type  = "post"
tags  = [ "Tag1", "Tag2" ]
+++
```

`author` and `draft` are optional. Tags use title case for proper nouns (`.NET`, `Azure`, `CSharp`) and lower case otherwise.

## Blog post naming

Posts are Markdown files in `src/blog/content/posts/` following the pattern `YYYY-MM-DD_Title.md` (older posts use underscore; newer posts use hyphen `YYYY-MM-DD-Title.md`).
