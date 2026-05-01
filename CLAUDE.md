# BriefingRoom for DCS — Claude Code Instructions

Before writing any code, read:
- **`CONTRIBUTING.md`** — commit conventions, AI tagging, test requirements
- **`.github/copilot-instructions.md`** — full tech stack, constraints, and language-specific rules

## Critical rules

- Commit messages use `Added:` / `Updated:` / `Fixed:` / `Removed:` prefixes so they can be used directly as changelog entries.
- If you generate code that ends up in a commit, it must be marked `(AI Written)` in the commit subject line.
- Never hardcode user-facing strings — use `Database/Language/`.
- No new external dependencies without explicit approval.
- Write tests against original behaviour before changing it (`src/Tests/`).
