# Context Efficiency

## Principles

1. **Every token must justify its presence** - Don't explain what Claude already knows
2. **Reference, don't duplicate** - Point to actual code files instead of copying patterns inline
3. **Complement the rules file** - Skills add patterns; `rules/claude-instructions.md` has non-negotiables
4. **Use tables over prose** - Tables are denser and more scannable

## What NOT to Include in Skills

- Generic programming concepts Claude already knows (e.g., "what is a class")
- Full API documentation for well-known libraries
- Content already covered in `rules/claude-instructions.md`
- Verbose explanations when a code example suffices

## What TO Include in Skills

- Repository-specific conventions (e.g., Mapperly `MapperBase<T,U>` pattern)
- File paths to reference implementations
- Decision trees for choosing between approaches
- Common mistakes specific to ASP.NET Zero
- Exact class/method names used in this codebase

## Reference Pattern

Instead of embedding large code blocks:

```markdown
## App Service Pattern

Inherit from `HCMv3AppServiceBase`. See actual implementation:
- **Reference**: `aspnet-core/src/HCMv3.Application/Editions/EditionAppService.cs`

Key methods: `GetAll`, `GetForEdit`, `Create`, `Update`, `Delete`
```

This keeps the skill small (~100 tokens) while pointing Claude to the real code (~500+ lines) when needed.

## Token Budget Guidelines

| Artifact | Target Tokens | Max Tokens |
|----------|--------------|------------|
| Agent | 500 | 1,000 |
| Skill (SKILL.md) | 2,000 | 5,000 |
| Command | 800 | 2,000 |
| Knowledge file | 1,000 | 3,000 |
| Index file | 500 | 1,500 |
