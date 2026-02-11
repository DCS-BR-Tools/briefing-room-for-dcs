# GitHub Actions Improvements

## Context
- Workflows: `.github/workflows/`
- Release workflow: `dotnet-release.yml`
- Beta workflow: `dotnet.yml`
- Runs on: `windows-latest` (build), `ubuntu-latest` (docker)

## Prompt

```
You are a GitHub Actions CI/CD expert.

CONSTRAINTS:
- Use only well-maintained, official, or widely-adopted actions
- Pin action versions to major version (e.g., @v4)
- Prefer built-in GitHub features over third-party actions
- Changes must not break existing functionality

TASK: [describe the workflow change]

Provide:
1. Exact YAML changes with file path
2. Explanation of what each change does
3. Any required secrets or permissions
4. How to test the change
```

## Quick Prompts

**Reduce build time:**
```
Analyze .github/workflows/ and suggest changes to reduce CI time. Focus on caching, parallelization, and conditional steps. Show exact YAML changes.
```

**Add workflow:**
```
Create a workflow for [PURPOSE]. Use existing patterns from dotnet-release.yml. Minimize external actions. Include:
- Trigger conditions
- Required secrets (list them, don't assume they exist)
- Error handling
```

**Fix failing workflow:**
```
Workflow [NAME] fails with: [ERROR]

Diagnose the issue and provide:
1. Root cause
2. Exact YAML fix
3. How to verify the fix locally if possible
```

**Security audit:**
```
Review .github/workflows/ for security issues:
- Unpinned actions
- Exposed secrets
- Missing permission restrictions
- Script injection risks

Provide fixes with exact YAML changes.
```

**Matrix build:**
```
Convert [WORKFLOW] to use matrix strategy for [VARIATIONS]. Ensure artifacts are correctly named per matrix combination.
```
