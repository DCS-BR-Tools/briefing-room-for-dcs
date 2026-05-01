# BriefingRoom for DCS — Cursor Rules

Before writing any code, read CONTRIBUTING.md and .github/copilot-instructions.md.

## Commit conventions
- Use prefixes: Added: / Updated: / Fixed: / Removed:
- Commits with AI-generated code must end with (AI Written)

## Hard constraints
- No new NuGet/npm dependencies without approval
- No hardcoded user-facing strings — use Database/Language/
- Tests go in src/Tests/; write them against original behaviour first
- Lua targets 5.1 (DCS World) — no goto, no bitwise operators, no _ENV
- Use System.Text.Json, not Newtonsoft
- Build: dotnet build src/BriefingRoom.sln
