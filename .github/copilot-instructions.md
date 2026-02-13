# BriefingRoom for DCS - Copilot Instructions

## Project Overview
BriefingRoom is a mission generator for DCS World (Digital Combat Simulator).

## Tech Stack

| Component | Version/Details | Location |
|-----------|-----------------|----------|
| .NET | 10 | `src/` |
| Solution | `src/BriefingRoom.sln` | - |
| Main Library | `src/BriefingRoom/` | - |
| Web Project | `src/Web/Web.csproj` | - |
| Lua | 5.1 (DCS World) | `Include/Lua/` |
| Docker | Multi-stage build | `Dockerfile` |
| CI/CD | GitHub Actions | `.github/workflows/` |

## Runtime Environments
- **Windows**: 64-bit executable
- **Docker**: `mcr.microsoft.com/dotnet/aspnet:10.0` (Linux)

## Build Commands
```bash
# Development build
dotnet build src/BriefingRoom.sln

# Release publish
rm -r ./Release;  dotnet publish src/BriefingRoom.sln -c Release -p:Flavor=EXE
```

## Key Directories

| Path | Purpose |
|------|---------|
| `Database/` | INI configuration files |
| `Database/Language/` | Translation files (multi-language support) |
| `DatabaseJSON/` | JSON data files (units, templates, theaters) |
| `Include/Lua/` | Lua scripts for DCS missions |
| `Include/Html/` | HTML templates |
| `src/BriefingRoom/code_docs/` | Code documentation |
| `.github/workflows/` | CI/CD workflows |

## Constraints (ALWAYS FOLLOW)

1. **No new dependencies** - Do NOT add NuGet packages, npm packages, or external libraries without explicit user approval
2. **Use built-in features** - Prefer .NET/Lua standard library over third-party solutions
3. **Follow existing patterns** - Read existing code before implementing; ask if patterns are unclear
4. **Exact paths required** - Always provide complete file paths for any changes
5. **Backward compatible** - Changes must not break existing functionality
6. **Self-contained** - Users should not need to install dependencies manually
7. **Performance & size** - Consider runtime performance and download/output size impact
8. **Human-readable errors** - Error messages must be clear and understandable to end users
9. **Use translations** - User-facing text must use the existing translation system in `Database/Language/`; do not hardcode strings
10. **Update documentation** - Keep relevant documentation in `src/BriefingRoom/code_docs/` up to date when making changes
11. **Unit testing** - When modifying a function, first write/run tests against the original behavior, then make changes, then update tests accordingly
12. **Publish entire project** - Always clean and publish the entire project, not individual files, to ensure all dependencies are included

## Language-Specific Notes

### .NET 10
- Use modern C# features (records, pattern matching, file-scoped namespaces)
- Prefer `Span<T>`, `Memory<T>` for performance-critical code
- Use `System.Text.Json` (not Newtonsoft)

### Lua 5.1 (DCS World)
- **No Lua 5.2+ features**: no `goto`, no bitwise operators, no `_ENV`
- Use `pairs()`/`ipairs()` for iteration
- Always use `local` variables
- DCS-specific APIs are available in mission context

### Docker
- Multi-stage builds required (build stage + runtime stage)
- Base images: `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`
- Minimize final image size

### GitHub Actions
- Pin actions to major versions (e.g., `@v4`)
- Prefer official/widely-adopted actions
- Windows runner for builds, Ubuntu for Docker

## Before Making Changes

1. **Read relevant files** to understand existing patterns
2. **Identify similar implementations** in the codebase
3. **State assumptions** before implementing
4. **Provide complete code** (no pseudocode or placeholders)

## Additional Prompts
For task-specific prompts, see `.prompts/` folder:
- `dotnet-build.md` - Build optimization
- `dotnet-code.md` - Feature development
- `docker.md` - Container configuration
- `github-actions.md` - CI/CD workflows
- `lua-scripting.md` - DCS World scripts
