---
name: angular-primeng-table-patterns
description: "PrimeNG table patterns for ASP.NET Zero (simple and paginated, lazy loading)"
---

# PrimeNG Table Patterns

## CRITICAL Rules

1. **Do NOT call `getRecords()` in `ngOnInit()`** when `(onLazyLoad)` is set — the table triggers it automatically
2. **Do NOT use `[responsive]`** attribute on tables

## Simple Table (No Pagination)

For small datasets where all records fit on one page:

### Component

```typescript
export class RolesComponent extends AppComponentBase {
    getRoles(): void {
        this.primengTableHelper.showLoadingIndicator();

        this._roleService
            .getRoles(new GetRolesInput({ permissions: selectedPermissions }))
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.items.length;
                this.primengTableHelper.hideLoadingIndicator();
            });
    }
}
```

### Template

```html
<div [busyIf]="primengTableHelper.isLoading()">
    <p-table #dataTable
        [value]="primengTableHelper.records"
        [lazy]="true"
        (onLazyLoad)="getRoles()"
        [scrollable]="true">

        <ng-template pTemplate="header">
            <tr>
                <th>{{ 'Actions' | localize }}</th>
                <th pSortableColumn="displayName">{{ 'Name' | localize }}</th>
            </tr>
        </ng-template>

        <ng-template pTemplate="body" let-record>
            <tr>
                <td><!-- action menu --></td>
                <td>{{ record.displayName }}</td>
            </tr>
        </ng-template>
    </p-table>
</div>
```

## Paginated Table (With Pagination)

For large datasets with server-side paging:

### Component

```typescript
export class ProductsComponent extends AppComponentBase {
    readonly filterText = signal<string>('');

    getProducts(event?: any): void {
        this.primengTableHelper.showLoadingIndicator();

        this._productService
            .getProducts(
                new GetProductsInput({
                    filter: this.filterText(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.hideLoadingIndicator();
            });
    }
}
```

**Critical**: Always pass a single DTO input object (e.g. `new GetProductsInput({...})`), never individual parameters. NSwag generates `[HttpPost]` list methods that expect a typed DTO, not query-string args.

**Critical**: `getMaxResultCount` and `getSkipCount` require the `event` parameter when called from `onLazyLoad` or `onPageChange` to compute the correct page offset.

### Template

```html
<div class="mb-4">
    <input type="text"
        class="form-control"
        [placeholder]="'SearchWithThreeDot' | localize"
        [ngModel]="filterText()"
        (ngModelChange)="filterText.set($event)" />
</div>

<div [busyIf]="primengTableHelper.isLoading()">
    <p-table #dataTable
        [value]="primengTableHelper.records"
        [lazy]="true"
        (onLazyLoad)="getProducts($event)"
        [scrollable]="true"
        [rows]="primengTableHelper.defaultRecordsCountPerPage">

        <ng-template pTemplate="header">
            <tr>
                <th style="width: 130px">{{ 'Actions' | localize }}</th>
                <th pSortableColumn="name">{{ 'Name' | localize }}</th>
                <th pSortableColumn="price">{{ 'Price' | localize }}</th>
            </tr>
        </ng-template>

        <ng-template pTemplate="body" let-record>
            <tr>
                <td><!-- action menu --></td>
                <td>{{ record.name }}</td>
                <td>{{ record.price }}</td>
            </tr>
        </ng-template>
    </p-table>

    <p-paginator #paginator
        [rows]="primengTableHelper.defaultRecordsCountPerPage"
        [totalRecords]="primengTableHelper.totalRecordsCount"
        (onPageChange)="getProducts($event)">
    </p-paginator>
</div>
```

**Critical**: Signal-based filter must use `[ngModel]="filterText()"` + `(ngModelChange)="filterText.set($event)"`. Do **NOT** use `[(ngModel)]="filterText"` — that binds to the signal function reference, not its value.

## Action Menu (TieredMenu)

```html
<td>
    <p-tieredMenu #menu [model]="activeMenuItems()" [popup]="true"></p-tieredMenu>
    <button pButton
        icon="pi pi-cog"
        (click)="showMenu($event, record, menu)">
    </button>
</td>
```

```typescript
readonly activeMenuItems = signal<MenuItem[]>([]);

showMenu(event: Event, record: ProductListDto, menu: any): void {
    this.activeMenuItems.set(this.getMenuItems(record));
    menu.toggle(event);
}

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

## PrimengTableHelper Methods

| Method | Purpose |
|--------|---------|
| `showLoadingIndicator()` | Show spinner |
| `hideLoadingIndicator()` | Hide spinner |
| `getSorting(table)` | Get current sort string |
| `getMaxResultCount(paginator, event)` | Get page size (pass `event` from lazy/page change) |
| `getSkipCount(paginator, event)` | Get items to skip (pass `event` from lazy/page change) |
| `isLoading()` | Signal: loading state |
| `records` | Current page data |
| `totalRecordsCount` | Total records for paginator |
| `defaultRecordsCountPerPage` | Default page size |

## Reference Files

- **Simple table**: `angular/src/app/admin/roles/roles.component.ts` + `.html`
- **Paginated table**: `angular/src/app/admin/users/users.component.ts` + `.html`

## Checklist

- [ ] `[lazy]="true"` on p-table
- [ ] `(onLazyLoad)="getRecords()"` bound
- [ ] Do NOT call `getRecords()` in `ngOnInit()`
- [ ] `[busyIf]="primengTableHelper.isLoading()"` on container
- [ ] TieredMenu with `[popup]="true"` for actions
- [ ] Permission checks on menu items
- [ ] No `[responsive]` attribute
