# GitHub Actions

> Base context from [copilot-instructions.md](../.github/copilot-instructions.md) is auto-included.

## Workflow Files
- Release: `.github/workflows/dotnet-release.yml`
- Beta: `.github/workflows/dotnet.yml`

## Quick Prompts

**Reduce build time:**
```
Analyze .github/workflows/ and suggest changes to reduce CI time. Focus on caching, parallelization, and conditional steps. Show exact YAML changes.
```

**Add workflow:**
```
Create a workflow for [PURPOSE]. Use existing patterns from dotnet-release.yml. Include trigger conditions, required secrets, and error handling.
```

**Fix failing workflow:**
```
Workflow [NAME] fails with: [ERROR]

Diagnose and provide root cause, exact YAML fix, and how to verify.
```

**Security audit:**
```
Review .github/workflows/ for security issues: unpinned actions, exposed secrets, missing permissions, script injection. Provide fixes.
```

**Matrix build:**
```
Convert [WORKFLOW] to use matrix strategy for [VARIATIONS]. Ensure artifacts are correctly named per matrix combination.
```
