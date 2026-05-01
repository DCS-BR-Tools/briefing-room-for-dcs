# Contributing to BriefingRoom for DCS

Thank you for taking the time to contribute! Please read the guidelines below before opening a pull request.

---

## Code Quality

- Follow the patterns and conventions already established in the codebase — read nearby code before writing new code.
- Use modern C# features where appropriate (records, pattern matching, file-scoped namespaces, etc.).
- Keep changes focused. Avoid refactoring unrelated code in the same PR.
- User-facing strings must go through the translation system in `Database/Language/` — do not hardcode them.
- Do not introduce new NuGet, npm, or other external dependencies without prior discussion.

## Tests

- If you modify a function, check whether existing tests cover it. If they do not, add them.
- Write tests against the *original* behaviour first, then make your change, then update the tests if the behaviour intentionally changes.
- Tests live in `src/Tests/`. Follow the structure and naming conventions already there.

---

## Commit Messages

Releases are generated directly from commit history, so commit messages should be written as self-contained changelog lines. Use the same prefixes the changelog uses:

| Prefix | When to use |
|--------|-------------|
| `Added:` | New feature or content |
| `Updated:` | Change to existing behaviour, data, or dependency |
| `Fixed:` | Bug fix |
| `Removed:` | Feature or content that has been deleted |

**Examples:**
```
Added: Carrier group spawn option for Red coalition
Fixed: Campaign spawn points ignored theater bounds
Updated: Skynet Script to 3.4.0
```

Avoid vague messages like `"fix stuff"`, `"wip"`, or `"changes"`. Each commit should read like a bullet point someone would want to see in a release.

---

## AI-Generated Code

AI-assisted development is welcome. However, any commit that contains AI-generated code **must** be clearly marked so reviewers and the project history reflect this accurately.

Add `(AI Written)` to the end of the commit message subject line:

```
Added: Auto-detect player airbase from template (AI Written)
Fixed: Null reference in spawn point selection (AI Written)
```

This applies to any commit where AI tooling (GitHub Copilot, ChatGPT, Claude, etc.) produced a meaningful portion of the code. Minor AI-assisted autocompletion of a single line or boilerplate does not need to be flagged — use your judgement.

---

## Pull Requests

- Give the PR a clear title that summarises the change.
- Describe *what* changed and *why* in the PR description. If it fixes a reported issue, link to it.
- Keep PRs reasonably scoped. Large mixed-concern PRs are harder to review and merge.
- Make sure the project builds (`dotnet build src/BriefingRoom.sln`) and all tests pass before opening the PR.
