# Docker

> Base context from [copilot-instructions.md](../.github/copilot-instructions.md) is auto-included.

## Quick Prompts

**Reduce image size:**
```
Analyze the Dockerfile and suggest changes to reduce final image size. Consider removing unnecessary apt packages, alpine base compatibility, layer caching, cleaning apt cache. Show exact changes with size estimates.
```

**Improve build caching:**
```
Optimize Dockerfile layer caching for faster rebuilds. Show exact changes and explain cache invalidation triggers.
```

**Security hardening:**
```
Review Dockerfile for security issues: running as root, unnecessary packages, missing health checks, base image vulnerabilities. Provide fixes.
```

**Add health check:**
```
Add a health check to the Dockerfile for the ASP.NET Web application. Show exact HEALTHCHECK instruction.
```

**Debug build issues:**
```
Docker build fails with: [ERROR]

Diagnose and provide root cause, exact fix, and how to verify locally.
```

**Multi-platform build:**
```
Modify Dockerfile to support multi-platform builds (linux/amd64, linux/arm64). Show Dockerfile changes and docker buildx command.
```
