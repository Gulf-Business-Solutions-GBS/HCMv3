---
name: angular-modal-patterns
description: "Bootstrap modal dialog patterns for ASP.NET Zero Angular using AppBsModalDirective"
---

# Angular Modal Patterns

## Quick Start

Modal components use `AppBsModalDirective` with signal-based state:

### Component

```typescript
import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProductServiceProxy, ProductEditDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditProductModal',
    templateUrl: './create-or-edit-product-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditProductModalComponent extends AppComponentBase {
    private _productService = inject(ProductServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly product = signal<ProductEditDto>(new ProductEditDto());

    show(productId?: number): void {
        if (productId) {
            this._productService.getProductForEdit(productId).subscribe((result) => {
                this.product.set(result.product);
                this.active.set(true);
                this.modal().show();
            });
        } else {
            this.product.set(new ProductEditDto());
            this.active.set(true);
            this.modal().show();
        }
    }

    onShown(): void {
        document.getElementById('ProductName')?.focus();
    }

    save(): void {
        this.saving.set(true);

        const input = this.product();
        const operation = input.id
            ? this._productService.updateProduct(input)
            : this._productService.createProduct(input);

        operation
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
```

### Template

```html
<div appBsModal #createOrEditModal="bs-modal"
     [config]="{ backdrop: 'static', keyboard: !saving() }"
     (onShown)="onShown()"
     class="modal fade" tabindex="-1">

    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            @if (active()) {
                <form #productForm="ngForm" (ngSubmit)="save()">
                    <div class="modal-header">
                        <h4 class="modal-title">
                            {{ (product()?.id ? 'EditProduct' : 'CreateNewProduct') | localize }}
                        </h4>
                        <button type="button" class="btn-close"
                                (click)="close()"
                                [disabled]="saving()">
                        </button>
                    </div>

                    <div class="modal-body">
                        <div class="mb-3">
                            <label for="ProductName">{{ 'Name' | localize }}</label>
                            <input id="ProductName" type="text"
                                   class="form-control"
                                   [ngModel]="product()?.name"
                                   (ngModelChange)="updateProductName($event)"
                                   name="Name" required maxlength="256">
                        </div>
                    </div>

                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary"
                                (click)="close()"
                                [disabled]="saving()">
                            {{ 'Cancel' | localize }}
                        </button>
                        <button type="submit" class="btn btn-primary"
                                [buttonBusy]="saving()"
                                [disabled]="!productForm.form.valid || saving()">
                            <i class="fa fa-save"></i>
                            {{ 'Save' | localize }}
                        </button>
                    </div>
                </form>
            }
        </div>
    </div>
</div>
```

## Key Conventions

| Convention | Detail |
|------------|--------|
| Directive | `appBsModal` (not plain ngx-bootstrap `bsModal`) |
| Template ref | `#createOrEditModal="bs-modal"` |
| Backdrop | `[config]="{ backdrop: 'static', keyboard: !saving() }"` |
| Active guard | `@if (active()) { ... }` inside modal content |
| State signals | `active`, `saving`, `{entity}` as signals |
| Focus | `(onShown)="onShown()"` → `document.getElementById(...).focus()` |
| Close | Set `active(false)` then `modal().hide()` |
| Save output | `readonly modalSave = output<void>()` |

## Updating Signal-Based DTOs

Since signals are immutable references, update DTO properties like this:

```typescript
updateProductName(value: string): void {
    const current = this.product();
    const updated = new ProductEditDto();
    Object.assign(updated, current);
    updated.name = value;
    this.product.set(updated);
}
```

## Handling Luxon DateTime Fields (NSwag Proxies)

NSwag generates `DateTime` fields as Luxon `DateTime` objects, **not** native JS `Date`. Angular's `DatePipe` does **not** accept Luxon `DateTime` — using `| date` pipe will cause a `TS2769` type error.

### Correct pattern for date `<input type="date">` binding

**Template** — use `.toISODate()` to get an `'yyyy-MM-dd'` string:

```html
<input
    type="date"
    class="form-control"
    name="SaleDate"
    [ngModel]="sale().saleDate?.toISODate()"
    (ngModelChange)="updateSaleDate($event)"
    required />
```

**Component** — import `DateTime` from `luxon`, convert the string back to Luxon on change:

```typescript
import { DateTime } from 'luxon';

updateSaleDate(value: string): void {
    const current = this.sale();
    const updated = new SaleEditDto();
    Object.assign(updated, current);
    updated.saleDate = value ? DateTime.fromISO(value) : undefined as any;
    this.sale.set(updated);
}
```

**Do NOT** import `DatePipe` from `@angular/common` for Luxon DateTime fields. Do **NOT** use `| date` pipe with Luxon values.

### Displaying DateTime in tables (read-only)

Use the `LuxonFormatPipe` already available in ASP.NET Zero, or call `.toLocaleString()` directly:

```html
<td>{{ record.saleDate | luxonFormat:'DD' }}</td>
```

## Using Modal from Parent

```html
<!-- In parent component template -->
<createOrEditProductModal #createOrEditProductModal
    (modalSave)="getProducts()">
</createOrEditProductModal>
```

```typescript
// In parent component class
readonly createOrEditProductModal = viewChild<CreateOrEditProductModalComponent>('createOrEditProductModal');

createProduct(): void {
    this.createOrEditProductModal().show();
}

editProduct(id: number): void {
    this.createOrEditProductModal().show(id);
}
```

## Reference Files

- **Modal component**: `angular/src/app/admin/roles/create-or-edit-role-modal.component.ts`
- **Modal template**: `angular/src/app/admin/roles/create-or-edit-role-modal.component.html`

## Checklist

- [ ] Uses `AppBsModalDirective` (not plain bsModal)
- [ ] `active` and `saving` as `signal<boolean>(false)`
- [ ] Entity state as signal
- [ ] `@if (active())` guards modal content
- [ ] `(onShown)` focuses first input
- [ ] `backdrop: 'static'` prevents click-outside close
- [ ] `keyboard: !saving()` prevents ESC during save
- [ ] `output<void>()` for modalSave
- [ ] `finalize(() => this.saving.set(false))` in save pipe
