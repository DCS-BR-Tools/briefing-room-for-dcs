# Docker

## Context
- Dockerfile: `Dockerfile`
- Base: `mcr.microsoft.com/dotnet/sdk:10.0` (build), `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime)
- Multi-stage build
- Linux container with apt dependencies
- Publishes: `src/Web/Web.csproj`

## Prompt

```
You are a Docker expert for .NET applications.

CONSTRAINTS:
- Use only official Microsoft .NET images
- Multi-stage builds required (separate build/runtime)
- Minimize final image size
- Do NOT add unnecessary apt packages
- Keep existing COPY structure for Database, Include, Media folders

TASK: [describe the Docker change]

Provide:
1. Exact Dockerfile changes
2. Explanation of each change
3. Expected image size impact
4. Any security considerations
```

## Quick Prompts

**Reduce image size:**
```
Analyze the Dockerfile and suggest changes to reduce final image size. Consider:
- Removing unnecessary apt packages
- Using alpine base if compatible
- Optimizing layer caching
- Cleaning apt cache

Show exact Dockerfile changes with size estimates.
```

**Improve build caching:**
```
Optimize Dockerfile layer caching for faster rebuilds. Keep multi-stage structure. Show exact changes and explain cache invalidation triggers.
```

**Security hardening:**
```
Review Dockerfile for security issues:
- Running as root
- Unnecessary packages
- Missing health checks
- Base image vulnerabilities

Provide fixes with exact Dockerfile changes.
```

**Add health check:**
```
Add a health check to the Dockerfile for the ASP.NET Web application. Use built-in curl or wget, or .NET health endpoint. Show exact HEALTHCHECK instruction.
```

**Debug build issues:**
```
Docker build fails with: [ERROR]

Diagnose and provide:
1. Root cause
2. Exact Dockerfile fix
3. How to verify locally
```

**Multi-platform build:**
```
Modify Dockerfile to support multi-platform builds (linux/amd64, linux/arm64). Consider:
- Base image compatibility
- Native dependencies
- Build arguments

Show Dockerfile changes and docker buildx command.
```

## Dockerfile Quick Reference

```dockerfile
# Multi-stage pattern
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src src
RUN dotnet publish -c Release -o /app src/Web/Web.csproj

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Web.dll"]

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
  CMD curl -f http://localhost:80/health || exit 1

# Non-root user
RUN adduser --disabled-password --gecos '' appuser
USER appuser

# Clean apt cache
RUN apt update && apt install -y pkg \
    && rm -rf /var/lib/apt/lists/*
```
