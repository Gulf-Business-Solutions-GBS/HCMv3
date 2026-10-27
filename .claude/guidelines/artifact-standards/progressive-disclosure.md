# Progressive Disclosure Pattern

Load information in stages to minimize context usage while providing depth on-demand.

## Three-Level Loading

| Level | What Loads | Token Cost | When |
|-------|------------|------------|------|
| **1. Discovery** | `name` + `description` from front matter | ~100 tokens | Startup |
| **2. Instructions** | Full `SKILL.md` body | <5,000 tokens | When triggered by context |
| **3. Resources** | Reference files, templates | As needed | On explicit request |

## Patterns

### High-Level Guide with References

```markdown
# Entity Patterns

## Quick Start
[Immediate actionable code - 20-30 lines]

## Core Patterns
[Essential patterns - 50-100 lines]

## Advanced
**Multi-tenancy**: See knowledge/conventions/...
**Custom repositories**: See knowledge/implementations/...
```

Core SKILL.md stays under 500 lines; depth goes in `references/` or `knowledge/`.

### Conditional Details

```markdown
## Creating Entities
Inherit from FullAuditedEntity<int>. See aspnetzero-entity-patterns skill.

**For multi-tenant entities**: See aspnetzero-multitenancy-patterns skill.
```

Link to advanced content only when needed.

## File Size Limits

| File | Max Lines | If Exceeded |
|------|-----------|-------------|
| Agent `.md` | 150 | Extract to skill references |
| SKILL.md | 500 | Extract to `references/` subdirectory |
| Command `.md` | 200 | Extract templates to `references/` |
| Knowledge file | 400 | Split into sub-files |

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|--------------|---------|-----|
| Everything in one file | High token cost | Extract to references |
| Duplicated content | Maintenance burden | Single source, link elsewhere |
| No quick-start section | Slow to scan | Always lead with Quick Start |
