# Generate React CRUD

Generate React CRUD page with Ant Design table/modal, Metronic layout, NSwag service proxies, and permission guards.

Provide the entity name (e.g., `Product`).

## Prerequisites

- Backend app service and DTOs must exist
- NSwag service proxies must be regenerated if the backend service is new (`react/nswag/refresh.bat`)
- Permissions and localization keys must exist

## What It Does

1. **Creates list page** — `pages/admin/{feature}/index.tsx` with Ant Design `Table`, `PageHeader`, filters, action `Dropdown` menus
2. **Creates create/edit modal** — `pages/admin/{feature}/components/CreateOrEdit{Feature}Modal.tsx` with Ant Design `Modal`, `react-hook-form`
3. **Adds route** — Lazy-loaded `React.lazy()` import + `<Route>` in `AppRouter.tsx`
4. **Adds menu entry** — Item with `permissionName` in `appNavigation.tsx`

## Generated Files

```
react/src/pages/admin/{feature}/
  index.tsx                                   # List page with table
  components/
    CreateOrEdit{Feature}Modal.tsx             # Create/edit modal dialog
```

## Modified Files

```
react/src/routes/AppRouter.tsx                # Add lazy import + <Route>
react/src/lib/navigation/appNavigation.tsx    # Add menu item with permission
```

## Key Patterns

- Functional components with TypeScript (`React.FC`), all hooks
- `useServiceProxy(ServiceProxy, [])` for API calls
- `useDataTable<T>(fetchFn)` + Ant Design `Table` for paginated lists
- Ant Design `Modal` with `open` prop, parent controls visibility
- `usePermissions()` → `isGranted()` / `isGrantedAny()` on all action buttons
- `L("Key")` for all UI text
- `modal.confirm()` from `App.useApp()` for delete confirmations
- HTML `<button>` with Bootstrap/Metronic CSS classes (not Ant Design Button)
- `PageHeader` + `containerClass` + `card card-custom` layout structure

Refer to the `react-component-patterns`, `react-state-patterns`, and `react-permission-patterns` skills for detailed implementation guidance.

## Reference Files

- List page: `react/src/pages/admin/roles/index.tsx`
- Modal: `react/src/pages/admin/roles/components/CreateOrEditRoleModal.tsx`
- Paginated: `react/src/pages/admin/users/index.tsx`

## Example

```
/generate-react-crud Product
```
