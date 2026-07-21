---
description: "Explain code's layer, dependencies, architectural role, and how it connects to other layers"
allowed-tools: Read, Glob, Grep
argument-hint: "$FILE_PATH - Path to the file or code to explain"
---

# Code Explain

## Usage

`/explain:code-explain $ARGUMENTS`

## What It Does

Reads the specified file or code and explains:

1. **Which layer** it belongs to (Core, Application, EF, Web, Angular)
2. **What it does** — purpose and responsibilities
3. **Dependencies** — what it depends on and what depends on it
4. **Conventions** — how it follows (or violates) ASP.NET Zero patterns
5. **Data flow** — how data flows through this code

## Output Format

```markdown
## Code Explanation: {FileName}

### Layer
{Core | Application.Shared | Application | EntityFrameworkCore | Web | Angular}

### Purpose
{What this code does and why}

### Key Dependencies
- Depends on: [list of injected services/repositories]
- Used by: [list of callers if discoverable]

### Patterns Used
- {Pattern 1}: {how it's used}
- {Pattern 2}: {how it's used}

### Data Flow
{How data enters, transforms, and exits this code}
```
