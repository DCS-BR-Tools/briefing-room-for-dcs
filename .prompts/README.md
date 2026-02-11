# AI Prompt Templates

Quick-reference prompts for AI coding assistants. Designed to minimize hallucinations and avoid unnecessary dependencies.

## Usage

1. Copy the relevant prompt template
2. Replace `[PLACEHOLDERS]` with your specific task
3. Paste into your AI assistant

## Templates

| File | Purpose |
|------|---------|
| [dotnet-build.md](dotnet-build.md) | Build optimization, publish config, warnings |
| [dotnet-code.md](dotnet-code.md) | Features, refactoring, bug fixes, performance |
| [docker.md](docker.md) | Dockerfile optimization, security, multi-platform |
| [github-actions.md](github-actions.md) | CI/CD workflows, caching, security |
| [lua-scripting.md](lua-scripting.md) | DCS World Lua 5.1 scripts |

## Project Context

| Stack | Version | Location |
|-------|---------|----------|
| .NET | 10 | `src/` |
| Lua | 5.1 | `Include/Lua/` |
| GitHub Actions | - | `.github/workflows/` |

## Guidelines

- **No new packages** unless explicitly approved
- **Verify suggestions** against existing code patterns
- **Ask for file reads** before implementing to confirm patterns
- **Request exact paths** for all changes
