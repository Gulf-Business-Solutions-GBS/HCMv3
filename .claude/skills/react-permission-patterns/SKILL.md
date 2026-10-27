---
name: react-permission-patterns
description: "Permission-based rendering and route guards for ASP.NET Zero React"
---

# React Permission Patterns

## Permission Check Utility

```typescript
// utils/permissions.ts
export function isGranted(permissionName: string): boolean {
    return abp.auth.isGranted(permissionName);
}

export function isAnyGranted(...permissionNames: string[]): boolean {
    return permissionNames.some((p) => abp.auth.isGranted(p));
}
```

## Conditional Rendering

```tsx
import { isGranted } from '../utils/permissions';

const ProductActions: React.FC<{ product: Product }> = ({ product }) => (
    <>
        {isGranted('Pages.Products.Edit') && (
            <button onClick={() => onEdit(product)} className="btn btn-sm btn-primary">
                Edit
            </button>
        )}
        {isGranted('Pages.Products.Delete') && (
            <button onClick={() => onDelete(product)} className="btn btn-sm btn-danger">
                Delete
            </button>
        )}
    </>
);
```

## Create Button Guard

```tsx
{isGranted('Pages.Products.Create') && (
    <button onClick={() => setShowForm(true)} className="btn btn-primary">
        Create New Product
    </button>
)}
```

## Route Guard Component

```tsx
interface PermissionGuardProps {
    permission: string;
    children: React.ReactNode;
    fallback?: React.ReactNode;
}

const PermissionGuard: React.FC<PermissionGuardProps> = ({
    permission,
    children,
    fallback = null,
}) => {
    return isGranted(permission) ? <>{children}</> : <>{fallback}</>;
};

// Usage
<PermissionGuard permission="Pages.Products">
    <ProductsPage />
</PermissionGuard>
```

## Permission Strings

Must match `AppPermissions.cs` constant values exactly:

| Permission | String |
|------------|--------|
| View products | `'Pages.Products'` |
| Create | `'Pages.Products.Create'` |
| Edit | `'Pages.Products.Edit'` |
| Delete | `'Pages.Products.Delete'` |

## Checklist

- [ ] `isGranted()` check on all action buttons
- [ ] Create button guarded by Create permission
- [ ] Edit/Delete guarded by respective permissions
- [ ] Actions column hidden if no permissions
- [ ] Route-level guards for page access
