# .NET Code Changes

> Base context from [copilot-instructions.md](../.github/copilot-instructions.md) is auto-included.

## Quick Prompts

**Add feature:**
```
Implement [FEATURE]. First show me which existing files implement similar functionality so I can verify you understand the codebase patterns.
```

**Refactor:**
```
Refactor [CLASS/METHOD] to improve [readability/performance/maintainability]. Keep the same public API. Show before/after with explanation.
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
Optimize [METHOD/CLASS] for performance. Use .NET 10 features like Span<T>, stackalloc, or pooling where appropriate. Show benchmarks or explain expected improvement.
```
