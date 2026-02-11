# Lua Scripting

> Base context from [copilot-instructions.md](../.github/copilot-instructions.md) is auto-included.

## Quick Prompts

**Add mission feature:**
```
Create Lua script for [FEATURE]. Follow patterns in Include/Lua/. Include comments explaining DCS-specific functions used.
```

**Fix script:**
```
Fix this Lua 5.1 script: [CODE]
Error: [ERROR MESSAGE]

Provide what's wrong, why it fails, and corrected code.
```

**Optimize script:**
```
Optimize this Lua script for performance. Use local variables, table pre-allocation, avoid string concat in loops, cache function lookups. Show before/after.
```

**Convert to Lua 5.1:**
```
Convert this code to Lua 5.1 compatible: [CODE]

Remove any Lua 5.2+ features and provide equivalent implementation.
```

## Lua 5.1 Quick Reference

```lua
-- Table iteration
for k, v in pairs(tbl) do end  -- unordered
for i, v in ipairs(arr) do end -- array ordered

-- Error handling
pcall(func, args)
xpcall(func, errorHandler)
```
