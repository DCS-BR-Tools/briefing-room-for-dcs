# Lua Scripting

## Context
- Lua 5.1 (DCS World scripting environment)
- Scripts: `Include/Lua/`
- Mission file: `mission.lua`
- DCS-specific APIs available

## Prompt

```
You are a Lua 5.1 scripting expert for DCS World.

CONSTRAINTS:
- Lua 5.1 ONLY (no 5.2+ features like goto, bitwise operators, etc.)
- Use only DCS World built-in APIs and standard Lua libraries
- No external Lua modules or libraries
- Follow existing patterns in Include/Lua/

FORBIDDEN in Lua 5.1:
- goto statement
- Bitwise operators (use bit library if available)
- _ENV variable
- Empty statements

TASK: [describe the script or change]

Provide:
1. Complete Lua code (no pseudocode)
2. File path where it belongs
3. Any DCS-specific APIs used
4. How to test in DCS
```

## Quick Prompts

**Add mission feature:**
```
Create Lua script for [FEATURE]. Use only Lua 5.1 and DCS built-in APIs. Follow patterns in Include/Lua/. Include comments explaining DCS-specific functions used.
```

**Fix script:**
```
Fix this Lua 5.1 script: [CODE]
Error: [ERROR MESSAGE]

Provide:
1. What's wrong (specific line)
2. Why it fails
3. Corrected code
```

**Optimize script:**
```
Optimize this Lua script for performance:
[CODE]

Use Lua 5.1 best practices:
- Local variables over globals
- Table pre-allocation
- Avoid string concatenation in loops
- Cache function lookups

Show before/after with explanation.
```

**Convert to Lua 5.1:**
```
Convert this code to Lua 5.1 compatible:
[CODE]

Remove any Lua 5.2+ features and provide equivalent 5.1 implementation.
```

## Lua 5.1 Quick Reference

```lua
-- String formatting (no format specifier %q for Lua 5.1 patterns)
string.format("%s %d", str, num)

-- Table iteration
for k, v in pairs(tbl) do end  -- unordered
for i, v in ipairs(arr) do end -- array ordered

-- Table length (arrays only)
#myTable

-- Metatables
setmetatable(tbl, mt)
getmetatable(tbl)

-- Error handling
pcall(func, args)
xpcall(func, errorHandler)
```
