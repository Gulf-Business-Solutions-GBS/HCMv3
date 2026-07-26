import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProjectCostingSettingsListDto, ManageProjectCostingSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditManageProjectCostingSettingsModalComponent } from './create-or-edit-manage-project-costing-settings-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    templateUrl: './manage-project-costing-settings.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        NgClass,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditManageProjectCostingSettingsModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class ManageProjectCostingSettingsComponent extends AppComponentBase implements OnInit {
    private _service = inject(ManageProjectCostingSettingsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditManageProjectCostingSettingsModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getRecords().
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getProjectCostingSettingsList(
                this.primengTableHelper.getSorting(this.dataTable()),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event)
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
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: ProjectCostingSettingsListDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: ProjectCostingSettingsListDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.SystemSettings.ProjectCostingSettings.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.SystemSettings.ProjectCostingSettings.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: ProjectCostingSettingsListDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.deleteProjectCostingSettings(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
