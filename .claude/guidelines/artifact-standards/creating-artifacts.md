# Creating New Artifacts

## Before You Start

1. Check if an existing artifact already covers the topic
2. Determine artifact type: Agent (persona) vs Skill (knowledge) vs Command (action)
3. Read the naming conventions and file format standards

## Decision Matrix

| Need | Artifact Type | Why |
|------|--------------|-----|
| Role-specific behavior | Agent | Constrains Claude to a persona |
| Domain knowledge/patterns | Skill | Teaches patterns, auto-triggered |
| User-invoked action | Command | One-shot entry point with `$ARGUMENTS` |
| Multi-step workflow | Flow | Documents step sequences |
| Foundational concepts | Knowledge | Framework-independent principles |

## Templates

### Agent Template

```markdown
---
name: my-agent
description: "What this agent does in one line"
tools: Read, Glob, Grep
skills: skill-1, skill-2
keywords: [keyword1, keyword2]
---

# Agent Display Name

## Summary
One paragraph describing the agent's role.

## Scope
**Does**: List of responsibilities
**Does NOT**: Boundaries (defer to other agents)

## Workflow
1. Step one
2. Step two

## Constraints
- Constraint one
- Constraint two
```

### Skill Template

```markdown
---
name: my-skill
description: "What patterns this skill teaches"
tech_stack: [dotnet]
topics: [topic1]
depends_on: []
complements: []
keywords: [keyword1, keyword2]
---

# Skill Display Name

## Quick Start
Minimal example to get started immediately.

## Core Patterns
### Pattern 1
Description + reference file path.

### Pattern 2
Description + reference file path.

## Common Mistakes
| Mistake | Fix |
|---------|-----|
| ... | ... |

## Checklist
- [ ] Item one
- [ ] Item two
```

### Command Template

```markdown
---
description: "What this command does"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
argument-hint: "$ARG - Description"
---

# Command Name

## Usage
`/category:command-name <arguments>`

## What It Does
1. Step one
2. Step two

## Options
- `--flag`: Description

## Example
`/category:command-name MyEntity`
```

## Checklist Before Committing

- [ ] YAML front matter is valid
- [ ] File is under the line limit (agents: 150, skills: 500, commands: 200)
- [ ] Referenced skills/files actually exist
- [ ] No duplicate content with existing artifacts
- [ ] Quick Start or Summary section is present
- [ ] Code examples reference actual repo files
