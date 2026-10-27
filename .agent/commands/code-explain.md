# Code Explain

Explain code's layer, dependencies, architectural role, and how it connects to other layers.

## Usage

Provide the file path or select code before invoking this command.

## What It Does

Reads the specified file or code and explains:

1. **Which layer** it belongs to (Core, Application.Shared, Application, EntityFrameworkCore, Web, Angular, React)
2. **What it does** — purpose and responsibilities
3. **Dependencies** — what it depends on and what depends on it
4. **Conventions** — how it follows (or violates) ASP.NET Zero patterns
5. **Data flow** — how data flows through this code

## Output Format

```markdown
## Code Explanation: {FileName}

### Layer
{Core | Application.Shared | Application | EntityFrameworkCore | Web | Angular | React}

### Purpose
{What this code does and why}

### Key Dependencies
- Depends on: [list of injected services/repositories]
- Used by: [list of callers if discoverable]

### Patterns Used
- {Pattern}: {how it's used}

### Data Flow
{How data enters, transforms, and exits this code}

### Potential Issues
{Any convention violations or concerns}
```

## Example

```
/code-explain aspnet-core/src/HCMv3.Application/Editions/EditionAppService.cs
```
