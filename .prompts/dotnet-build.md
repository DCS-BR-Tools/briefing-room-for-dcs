# .NET Build Improvements

## Context
- .NET 10
- Solution: `src/BriefingRoom.sln`
- Projects in `src/` folder
- Project is built at project level using "dotnet publish src/BriefingRoom.sln -c Release -p:Flavor=EXE"

## Prompt

```
You are a .NET 10 build optimization expert.

CONSTRAINTS:
- Do NOT suggest new NuGet packages unless absolutely necessary
- Prefer built-in .NET features over third-party libraries
- Changes must be backward compatible
- Explain WHY each change improves build performance

TASK: [describe build issue or improvement goal]

Provide:
1. Specific file changes with exact paths
2. Expected improvement (build time, size, etc.)
3. Any trade-offs or risks
```

## Quick Prompts

**Reduce build time:**
```
Review the csproj files in src/ and suggest changes to reduce build time. Focus on incremental build improvements, parallelization, and removing unnecessary dependencies. No new packages.
```

**Reduce output size:**
```
Analyze publish configuration for src/Web/Web.csproj. Suggest trimming, single-file, and AOT options appropriate for .NET 10. Show exact csproj property changes.
```

**Fix build warnings:**
```
List all build warnings in the solution and provide fixes. Prioritize by severity. Show exact code changes.
```
