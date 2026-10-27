---
name: angular-permission-patterns
description: "Permission-based UI rendering patterns for ASP.NET Zero Angular"
---

# Angular Permission Patterns

## Quick Start

### Template: Single Permission

```html
@if ('Pages.Products.Create' | permission) {
    <button class="btn btn-primary" (click)="createProduct()">
        {{ 'CreateNewProduct' | localize }}
    </button>
}
```

### Template: Multiple Permissions (Any)

```html
@if (['Pages.Products.Edit', 'Pages.Products.Delete'] | permissionAny) {
    <th>{{ 'Actions' | localize }}</th>
}
```

### TypeScript: Permission Check

```typescript
if (this.permission.isGranted('Pages.Products.Edit')) {
    items.push({
        label: this.l('Edit'),
        icon: 'pi pi-pencil',
        command: () => this.editProduct(record),
    });
}
```

## Required Imports

```typescript
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    imports: [
        PermissionPipe,      // For single permission checks
        PermissionAnyPipe,   // For multi-permission checks (any)
    ],
})
```

## Patterns

### Conditional Create Button

```html
@if ('Pages.Products.Create' | permission) {
    <button class="btn btn-primary" (click)="createProduct()">
        <i class="pi pi-plus"></i>
        {{ 'CreateNewProduct' | localize }}
    </button>
}
```

### Conditional Action Menu Items

```typescript
getMenuItems(record: ProductListDto): MenuItem[] {
    const items: MenuItem[] = [];

    if (this.permission.isGranted('Pages.Products.Edit')) {
        items.push({
            label: this.l('Edit'),
            icon: 'pi pi-pencil',
            command: () => this.createOrEditModal().show(record.id),
        });
    }

    if (this.permission.isGranted('Pages.Products.Delete')) {
        items.push({
            label: this.l('Delete'),
            icon: 'pi pi-trash',
            command: () => this.deleteProduct(record),
        });
    }

    return items;
}
```

### Conditional Table Column

```html
@if (['Pages.Products.Edit', 'Pages.Products.Delete'] | permissionAny) {
    <th style="width: 130px">{{ 'Actions' | localize }}</th>
}
```

### Menu Item Visibility

```typescript
// In app-navigation.service.ts or similar
if (this._permissionService.isGranted('Pages.Products')) {
    menuItems.push(new MenuItem('Products', '/app/products'));
}
```

## Permission String Format

Permission strings must match the values in `AppPermissions.cs`:

| C# Constant | String Value | Angular Usage |
|-------------|-------------|---------------|
| `Pages_Products` | `"Pages.Products"` | `'Pages.Products'` |
| `Pages_Products_Create` | `"Pages.Products.Create"` | `'Pages.Products.Create'` |
| `Pages_Products_Edit` | `"Pages.Products.Edit"` | `'Pages.Products.Edit'` |
| `Pages_Products_Delete` | `"Pages.Products.Delete"` | `'Pages.Products.Delete'` |

## Reference Files

- **Permission in template**: `angular/src/app/admin/users/users.component.html`
- **Permission in code**: `angular/src/app/admin/roles/roles.component.ts`

## Checklist

- [ ] `PermissionPipe` imported in component
- [ ] `PermissionAnyPipe` imported if multi-permission check needed
- [ ] Create button guarded by Create permission
- [ ] Edit action guarded by Edit permission
- [ ] Delete action guarded by Delete permission
- [ ] Actions column hidden if no action permissions
- [ ] Permission strings match `AppPermissions.cs` values exactly
