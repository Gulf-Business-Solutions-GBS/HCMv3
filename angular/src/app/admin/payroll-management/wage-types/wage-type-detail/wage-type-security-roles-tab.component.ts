import { Component, DestroyRef, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetAllWageTypeSecurityRolesInput,
    WageTypeSecurityRoleDto,
    WageTypeSecurityRolesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CreateOrEditWageTypeSecurityRoleModalComponent } from '../../wage-type-security-roles/create-or-edit-wage-type-security-role-modal.component';

/** Security Roles tab of the wage type detail page: the roles allowed to see this wage type. */
@Component({
    selector: 'wageTypeSecurityRolesTab',
    templateUrl: './wage-type-security-roles-tab.component.html',
    imports: [
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditWageTypeSecurityRoleModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class WageTypeSecurityRolesTabComponent extends AppComponentBase implements OnInit {
    private _wageTypeSecurityRolesService = inject(WageTypeSecurityRolesServiceProxy);
    private _destroyRef = inject(DestroyRef);

    readonly wageTypeId = input.required<number>();

    readonly createOrEditModal = viewChild<CreateOrEditWageTypeSecurityRoleModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    private readonly _filterChanged = new Subject<string>();

    ngOnInit(): void {
        // No getRecords() here: the PrimeNG table's (onLazyLoad) triggers the first load.
        this._filterChanged
            .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this.getRecords());
    }

    onFilterChange(value: string): void {
        this.filterText.set(value);
        this._filterChanged.next(value);
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._wageTypeSecurityRolesService
            .getAll(
                new GetAllWageTypeSecurityRolesInput({
                    wageTypeId: this.wageTypeId(),
                    filter: this.filterText(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show(undefined, this.wageTypeId());
    }

    showMenu(event: Event, record: WageTypeSecurityRoleDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: WageTypeSecurityRoleDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.WageTypeSecurityRoles.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.wageTypeId()),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.WageTypeSecurityRoles.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: WageTypeSecurityRoleDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.roleName), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._wageTypeSecurityRolesService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
