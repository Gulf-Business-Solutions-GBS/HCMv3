import { Component, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CostRegisterDetailDto,
    CostRegisterDetailsServiceProxy,
    GetAllCostRegisterDetailsInput,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CostElementSlotsService } from '@app/shared/common/cost-element-slots/cost-element-slots.service';
import { CreateOrEditCostRegisterDetailModalComponent } from '../../cost-register-details/create-or-edit-cost-register-detail-modal.component';

/** Details tab of the cost register page: the cost distribution lines of this register only. */
@Component({
    selector: 'costRegisterDetailsTab',
    templateUrl: './cost-register-details-tab.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditCostRegisterDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class CostRegisterDetailsTabComponent extends AppComponentBase implements OnInit {
    private _detailsService = inject(CostRegisterDetailsServiceProxy);

    /** Columns for the configured cost element slots, captioned with their cost element types. */
    readonly costElementSlots = inject(CostElementSlotsService);

    readonly costRegisterId = input.required<number>();

    readonly createOrEditModal = viewChild<CreateOrEditCostRegisterDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // No getRecords() here: the PrimeNG table's (onLazyLoad) triggers the first load.
        this.costElementSlots.ensureLoaded().subscribe();
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._detailsService
            .getAll(
                new GetAllCostRegisterDetailsInput({
                    costRegisterId: this.costRegisterId(),
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
        this.createOrEditModal().show(undefined, this.costRegisterId());
    }

    showMenu(event: Event, record: CostRegisterDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: CostRegisterDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.CostRegisterDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.costRegisterId()),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.CostRegisterDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: CostRegisterDetailDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.wageTypeName), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._detailsService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
