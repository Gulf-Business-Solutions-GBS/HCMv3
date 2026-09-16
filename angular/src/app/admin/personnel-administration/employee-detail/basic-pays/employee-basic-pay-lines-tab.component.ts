import { Component, DestroyRef, OnInit, inject, input, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DependantUnit,
    EmployeeBasicPayDetailDto,
    EmployeeBasicPayDetailsServiceProxy,
    GetAllEmployeeBasicPayDetailsInput,
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
import { CreateOrEditEmployeeBasicPayDetailModalComponent } from '../../employee-basic-pay-details/create-or-edit-employee-basic-pay-detail-modal.component';

/** Wage Types tab of the basic pay detail view: the wage type lines whose amounts make up the basic pay. */
@Component({
    selector: 'employeeBasicPayLinesTab',
    templateUrl: './employee-basic-pay-lines-tab.component.html',
    imports: [
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditEmployeeBasicPayDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class EmployeeBasicPayLinesTabComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeBasicPayDetailsServiceProxy);
    private _destroyRef = inject(DestroyRef);

    readonly employeeBasicPayId = input.required<number>();

    /** Emitted after a line is saved or deleted, so the header (whose amount is the sum of its lines) can reload. */
    readonly changed = output<void>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeBasicPayDetailModalComponent>('createOrEditModal');
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

        this._service
            .getAll(
                new GetAllEmployeeBasicPayDetailsInput({
                    employeeBasicPayId: this.employeeBasicPayId(),
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

    onLineChanged(): void {
        this.reloadPage();
        this.changed.emit();
    }

    createRecord(): void {
        this.createOrEditModal().show(undefined, this.employeeBasicPayId());
    }

    dependantUnitName(unit: DependantUnit): string {
        const name = DependantUnit[unit];
        return name ? this.l(name) : '';
    }

    showMenu(event: Event, record: EmployeeBasicPayDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeBasicPayDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeBasicPayDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.employeeBasicPayId()),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeBasicPayDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeBasicPayDetailDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.wageTypeName), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onLineChanged();
                });
            }
        });
    }
}
