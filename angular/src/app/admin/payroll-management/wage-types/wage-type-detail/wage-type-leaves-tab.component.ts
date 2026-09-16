import { Component, DestroyRef, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDto,
    GetAllWageTypeLeavesInput,
    WageTypeLeaveDto,
    WageTypeLeavesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CreateOrEditWageTypeLeaveModalComponent } from '../../wage-type-leaves/create-or-edit-wage-type-leave-modal.component';

/** Leave Types tab of the wage type detail page: absence types whose leave pay / provision uses this wage type. */
@Component({
    selector: 'wageTypeLeavesTab',
    templateUrl: './wage-type-leaves-tab.component.html',
    imports: [
        BusyIfDirective,
        ButtonBusyDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditWageTypeLeaveModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class WageTypeLeavesTabComponent extends AppComponentBase implements OnInit {
    private _wageTypeLeavesService = inject(WageTypeLeavesServiceProxy);
    private _destroyRef = inject(DestroyRef);

    readonly wageTypeId = input.required<number>();

    readonly createOrEditModal = viewChild<CreateOrEditWageTypeLeaveModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);
    readonly addingAll = signal<boolean>(false);

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

        this._wageTypeLeavesService
            .getAll(
                new GetAllWageTypeLeavesInput({
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

    addAllLeaveTypes(): void {
        this.message.confirm(this.l('AddAllLeaveTypesConfirmation'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.addingAll.set(true);
            this._wageTypeLeavesService
                .addAllLeaveTypes(new EntityDto({ id: this.wageTypeId() }))
                .pipe(finalize(() => this.addingAll.set(false)))
                .subscribe((added) => {
                    this.notify.success(this.l('AddAllLeaveTypesResult', added ?? 0));
                    this.getRecords();
                });
        });
    }

    showMenu(event: Event, record: WageTypeLeaveDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: WageTypeLeaveDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.WageTypeLeaves.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.wageTypeId()),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.WageTypeLeaves.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: WageTypeLeaveDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.absenceTypeName), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._wageTypeLeavesService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
