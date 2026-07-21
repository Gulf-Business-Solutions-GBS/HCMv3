# Naming Conventions

## Artifact File Naming

| Artifact | Location | Naming | Example |
|----------|----------|--------|---------|
| Agent | `.claude/agents/{category}/{name}.md` | kebab-case | `aspnetzero-developer.md` |
| Skill | `.claude/skills/{name}/SKILL.md` | kebab-case folder, uppercase file | `mapperly-patterns/SKILL.md` |
| Command | `.claude/commands/{category}/{name}.md` | kebab-case | `generate/entity.md` |
| Flow | `.claude/flows/{name}.md` | kebab-case | `crud-implementation.md` |
| Knowledge | `.claude/knowledge/{area}/{topic}/{file}.md` | kebab-case | `conventions/naming-conventions.md` |
| Guideline | `.claude/guidelines/{category}/{name}.md` | kebab-case | `artifact-standards/progressive-disclosure.md` |
| Index | `.claude/{NAME}.md` | UPPER-KEBAB-CASE | `SKILL-INDEX.md` |

## Agent Categories

| Category | Purpose | Example Agents |
|----------|---------|----------------|
| `engineers/` | Write code | `aspnetzero-developer`, `angular-developer` |
| `architects/` | Design (read-only) | `backend-architect` |
| `reviewers/` | Review/test | `code-reviewer`, `qa-engineer` |
| `specialists/` | Focused tasks | `debugger` |

## Command Categories

| Category | Purpose | Example Commands |
|----------|---------|------------------|
| `generate/` | Scaffold code | `entity`, `appservice`, `angular-crud` |
| `feature/` | End-to-end features | `add-feature` |
| `review/` | Code quality checks | `pre-push`, `permissions` |
| `debug/` | Diagnosis | `smart-debug` |
| `explain/` | Code explanation | `code-explain` |
| `build/` | Build/tooling | `create-bundles`, `generate-service-proxies` |

## Skill Naming

- Use `{scope}-{topic}-patterns` format
- Prefix ASP.NET Zero specific skills with `aspnetzero-`
- Prefix Angular specific skills with `angular-`
- Prefix React specific skills with `react-`
- General skills use plain names: `clean-code-dotnet`, `api-design-patterns`
