---
description: "Generate Angular CRUD page with PrimeNG table, modal, routing, and menu entry"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
argument-hint: "$ENTITY_NAME - The entity to generate Angular CRUD for (e.g., Product)"
---

# Generate Angular CRUD

## Usage

`/generate:angular-crud $ARGUMENTS`

## Prerequisites

- Backend app service and DTOs must exist
- Service proxies must be generated (`npm run nswag` in `angular/`)

## What It Does

1. **Creates list component** — PrimeNG table with lazy loading, action menu, permission guards
2. **Creates modal component** — Create/edit form with AppBsModalDirective, signal-based state
3. **Adds route** to the appropriate routing module
4. **Adds menu entry** with permission check

## Generated Files

```
angular/src/app/{area}/{feature}/
  {feature}.component.ts
  {feature}.component.html
  create-or-edit-{feature}-modal.component.ts
  create-or-edit-{feature}-modal.component.html
```

## Workflow

1. Read the backend DTOs to discover properties for the table and form
2. Apply `angular-component-patterns` — create standalone components
3. Apply `angular-primeng-table-patterns` — create table with lazy loading
4. Apply `angular-modal-patterns` — create CRUD modal
5. Apply `angular-service-proxy-patterns` — wire NSwag proxy calls
6. Apply `angular-permission-patterns` — add permission guards
7. Add route and menu entry
8. Build: `npm run build` in `angular/`

## Skills Used

| Step | Skill |
|------|-------|
| Component structure | `angular-component-patterns` |
| Table | `angular-primeng-table-patterns` |
| Modal | `angular-modal-patterns` |
| Service calls | `angular-service-proxy-patterns` |
| Permissions | `angular-permission-patterns` |

## Options

- `--area admin` — Place in admin area (default)
- `--area main` — Place in main area
- `--no-pagination` — Simple table without paginator
- `--paginated` — Paginated table (default)

## Example

```
/generate:angular-crud Product
/generate:angular-crud Product --area admin --paginated
```

## Next Steps

- Run `npm run nswag` if proxies aren't generated yet
- Run `npm run build` to verify
