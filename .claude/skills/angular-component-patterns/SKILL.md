---
name: angular-component-patterns
description: "Angular 21 standalone component patterns for ASP.NET Zero with signals, inject(), and AppComponentBase"
---

# Angular Component Patterns

## Quick Start

All Angular components in ASP.NET Zero use **standalone components** with **signal-based state**:

```typescript
import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';

@Component({
    templateUrl: './products.component.html',
    animations: [appModuleAnimation()],
    imports: [
        BusyIfDirective,
        LocalizePipe,
        PermissionPipe,
        // ... other imports
    ],
})
export class ProductsComponent extends AppComponentBase implements OnInit {
    private _productService = inject(ProductServiceProxy);

    // Signals for state
    readonly filter = signal<string>('');

    ngOnInit(): void {
        // Setup logic (but NOT data fetching if using lazy-loaded table)
    }

    getProducts(): void {
        // Data fetching logic
    }
}
```

## Key Conventions

| Pattern | Old (Decorator) | New (Signal-based) |
|---------|-----------------|---------------------|
| Dependency Injection | `constructor(private svc: Service)` | `private _svc = inject(Service)` |
| ViewChild | `@ViewChild('ref') ref: Type` | `readonly ref = viewChild<Type>('ref')` |
| Output | `@Output() event = new EventEmitter()` | `readonly event = output<void>()` |
| State | `property: Type` | `readonly property = signal<Type>(value)` |
| Read signal | `this.property` | `this.property()` |
| Set signal | `this.property = value` | `this.property.set(value)` |

## AppComponentBase Utilities

Extends `AppComponentBase` to get:

| Utility | Usage | Purpose |
|---------|-------|---------|
| `this.l('Key')` | Localization | Translate text |
| `this.permission` | Permission checking | `.isGranted('Permission.Name')` |
| `this.notify` | Notifications | `.info()`, `.success()`, `.error()` |
| `this.message` | Dialogs | `.confirm()`, `.info()` |
| `this.primengTableHelper` | Table helper | PrimeNG table state management |

## Common Imports

```typescript
// Always needed
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';

// For permissions
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

// For tables
import { TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';

// For forms
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';

// For service proxies
import { ProductServiceProxy } from '@shared/service-proxies/service-proxies';
```

## File Location

```
angular/src/app/{area}/{feature}/
  {feature}.component.ts             # List component
  {feature}.component.html           # List template
  create-or-edit-{feature}-modal.component.ts    # Modal
  create-or-edit-{feature}-modal.component.html  # Modal template
```

## Reference Files

- **List component**: `angular/src/app/admin/roles/roles.component.ts`
- **Modal component**: `angular/src/app/admin/roles/create-or-edit-role-modal.component.ts`

## Checklist

- [ ] Standalone component (no NgModule)
- [ ] `imports` array in `@Component`
- [ ] `inject()` for DI (not constructor injection)
- [ ] `signal()` for state, `viewChild()` for refs, `output()` for events
- [ ] Extends `AppComponentBase`
- [ ] `animations: [appModuleAnimation()]`
- [ ] Import `LocalizePipe`, `PermissionPipe`, `BusyIfDirective`
