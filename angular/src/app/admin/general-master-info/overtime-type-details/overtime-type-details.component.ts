import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    OvertimeTypeDetailDto,
    OvertimeTypeDetailsServiceProxy,
    GetAllOvertimeTypeDetailsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditOvertimeTypeDetailModalComponent } from './create-or-edit-overtime-type-detail-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './overtime-type-details.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditOvertimeTypeDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class OvertimeTypeDetailsComponent extends AppComponentBase implements OnInit {
    private _overtimeTypeDetailsService = inject(OvertimeTypeDetailsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditOvertimeTypeDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getOvertimeTypeDetails().
    }

    getOvertimeTypeDetails(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._overtimeTypeDetailsService
            .getAll(
                new GetAllOvertimeTypeDetailsInput({
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
        this.getOvertimeTypeDetails({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createOvertimeTypeDetail(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: OvertimeTypeDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: OvertimeTypeDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.GeneralMasterInfo.OvertimeTypeDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.GeneralMasterInfo.OvertimeTypeDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteOvertimeTypeDetail(record),
            });
        }

        return items;
    }

    deleteOvertimeTypeDetail(record: OvertimeTypeDetailDto): void {
        this.message.confirm(this.l('OvertimeTypeDetailDeleteWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._overtimeTypeDetailsService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
