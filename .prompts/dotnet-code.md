# .NET Code Changes

## Context
- .NET 10
- Source: `src/` folder
- Main library: `src/BriefingRoom/`

## Prompt

```
You are a .NET 10 developer. Follow these rules strictly:

CONSTRAINTS:
- Use ONLY .NET 10 built-in APIs and existing project dependencies
- Do NOT add new NuGet packages without explicit approval
- Follow existing code patterns in the codebase
- If unsure about existing patterns, ASK before implementing

TASK: [describe the feature or change]

Before coding:
1. List files you need to read to understand existing patterns
2. Identify existing similar implementations to follow
3. State any assumptions

Provide:
1. Exact file paths for all changes
2. Complete code blocks (no pseudocode)
3. Any required test changes
```

## Quick Prompts

**Add feature:**
```
Implement [FEATURE]. First show me which existing files implement similar functionality so I can verify you understand the codebase patterns. Then provide implementation using only existing dependencies.
```

**Refactor:**
```
Refactor [CLASS/METHOD] to improve [readability/performance/maintainability]. Keep the same public API. Use only .NET 10 built-in features. Show before/after with explanation.
```

**Fix bug:**
```
Fix: [describe bug]
1. Identify the root cause with specific file and line
2. Explain why current code fails
3. Provide minimal fix (smallest change that resolves the issue)
4. List any edge cases the fix handles
```

**Performance:**
```
Optimize [METHOD/CLASS] for performance. Use .NET 10 features like Span<T>, stackalloc, or pooling where appropriate. No external libraries. Show benchmarks or explain expected improvement.
```
