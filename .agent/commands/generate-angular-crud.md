# Generate Angular CRUD

Generate Angular CRUD page with PrimeNG table, modal, routing, and menu entry.

Provide the entity name (e.g., `Product`).

## Prerequisites

- Backend app service and DTOs must exist
- Service proxies must be generated (`npm run nswag` in `angular/`)

## What It Does

1. **Creates list component** — PrimeNG table with lazy loading, TieredMenu actions, permission guards
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

## Key Patterns

- Standalone components (no NgModule), `inject()` for DI, `signal()` for state
- PrimeNG `p-table` with `[lazy]="true"` and `(onLazyLoad)` — NOT called in `ngOnInit()`
- `AppBsModalDirective` for modals, `@if (active())` guard
- `| localize` pipe for all text, `| permission` pipe for access control
- `finalize()` on all service calls
- Service proxy from `@shared/service-proxies/service-proxies`

Refer to the `angular-component-patterns`, `angular-primeng-table-patterns`, `angular-modal-patterns`, `angular-permission-patterns`, and `angular-service-proxy-patterns` skills for detailed implementation guidance.

## Reference Files

- List: `angular/src/app/admin/roles/roles.component.ts`
- Modal: `angular/src/app/admin/roles/create-or-edit-role-modal.component.ts`
- Paginated: `angular/src/app/admin/users/users.component.ts`

## Example

```
/generate-angular-crud Product
```
