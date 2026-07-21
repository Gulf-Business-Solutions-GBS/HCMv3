# File Format Standards

## YAML Front Matter

All agents, skills, and commands require YAML front matter delimited by `---`.

### Agent Front Matter

```yaml
---
name: agent-name
description: "One-line description"
tools: Read, Write, Edit, Bash, Glob, Grep
skills: skill-1, skill-2
keywords: [keyword1, keyword2]
---
```

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | kebab-case identifier |
| `description` | Yes | One-line, quoted |
| `tools` | Yes | Comma-separated tool whitelist (least privilege) |
| `skills` | Yes | Skills this agent references |
| `keywords` | No | Trigger keywords for auto-selection |

### Skill Front Matter

```yaml
---
name: skill-name
description: "One-line description"
tech_stack: [dotnet, abp, efcore]
topics: [entities, repositories]
depends_on: []
complements: [other-skill]
keywords: [entity, domain, aggregate]
---
```

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | kebab-case, matches folder name |
| `description` | Yes | One-line, quoted |
| `tech_stack` | Yes | Technologies involved |
| `topics` | Yes | Topics covered |
| `depends_on` | No | Required prerequisite skills |
| `complements` | No | Skills that work well together |
| `keywords` | Yes | Context trigger keywords |

### Command Front Matter

```yaml
---
description: "One-line description"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
argument-hint: "$ENTITY_NAME - The entity name"
---
```

| Field | Required | Description |
|-------|----------|-------------|
| `description` | Yes | One-line, quoted |
| `allowed-tools` | Yes | Tools the command may use |
| `argument-hint` | No | Describes expected `$ARGUMENTS` |

## Markdown Body Conventions

- Use ATX headers (`#`, `##`, `###`)
- Use tables for structured data
- Use fenced code blocks with language hints (```csharp, ```typescript, ```xml)
- Include reference file paths so Claude can read actual code
- Lead with a Quick Start or Summary section
- Keep code examples minimal; reference actual repo files for full patterns
