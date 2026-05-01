# BriefingRoom for DCS — Agent Instructions

Before writing any code, read:
- **`CONTRIBUTING.md`** — commit conventions, AI tagging, test requirements
- **`.github/copilot-instructions.md`** — full tech stack, constraints, and language-specific rules

## Critical rules

- Commit messages use `Added:` / `Updated:` / `Fixed:` / `Removed:` prefixes so they can be used directly as changelog entries.
- Any commit containing AI-generated code must include `(AI Written)` in the subject line.
- Never hardcode user-facing strings — use `Database/Language/`.
- No new external dependencies without explicit approval.
- Write tests against original behaviour before changing it (`src/Tests/`).

## Key directories

| Path | Purpose |
|------|---------|
| `src/BriefingRoom/` | Main .NET library |
| `Database/` | INI configuration |
| `Database/Language/` | Translation files |
| `DatabaseJSON/` | JSON unit/theater data |
| `Include/Lua/` | DCS mission scripts (Lua 5.1) |
| `src/Tests/` | Unit tests |
