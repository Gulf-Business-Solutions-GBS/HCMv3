import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetAllManpowerPlanningsInput,
    ManpowerPlanningDto,
    ManpowerPlanningsServiceProxy,
    ManpowerPlanningStatus,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditManpowerPlanningModalComponent } from './create-or-edit-manpower-planning-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    templateUrl: './manpower-plannings.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        NgClass,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditManpowerPlanningModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class ManpowerPlanningsComponent extends AppComponentBase implements OnInit {
    private _service = inject(ManpowerPlanningsServiceProxy);
    private _router = inject(Router);

    readonly createOrEditModal = viewChild<CreateOrEditManpowerPlanningModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    readonly ManpowerPlanningStatus = ManpowerPlanningStatus;

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getManpowerPlannings().
    }

    getManpowerPlannings(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllManpowerPlanningsInput({
                    organizationUnitId: undefined,
                    periodId: undefined,
                    status: undefined as any,
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
        this.getManpowerPlannings({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createManpowerPlanning(): void {
        this.createOrEditModal().show();
    }

    viewDetails(record: ManpowerPlanningDto): void {
        this._router.navigate(['/app/admin/manpower-plannings', record.id, 'details']);
    }

    statusLabel(status: ManpowerPlanningStatus): string {
        switch (status) {
            case ManpowerPlanningStatus.Draft:
                return this.l('Draft');
            case ManpowerPlanningStatus.Submitted:
                return this.l('Submitted');
            case ManpowerPlanningStatus.Approved:
                return this.l('Approved');
            case ManpowerPlanningStatus.Rejected:
                return this.l('Rejected');
            default:
                return '';
        }
    }

    statusBadgeClass(status: ManpowerPlanningStatus): string {
        switch (status) {
            case ManpowerPlanningStatus.Approved:
                return 'badge-success';
            case ManpowerPlanningStatus.Rejected:
                return 'badge-danger';
            case ManpowerPlanningStatus.Submitted:
                return 'badge-warning';
            default:
                return 'badge-secondary';
        }
    }

    showMenu(event: Event, record: ManpowerPlanningDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: ManpowerPlanningDto): MenuItem[] {
        const items: MenuItem[] = [];

        items.push({
            label: this.l('Details'),
            icon: 'pi pi-list',
            command: () => this.viewDetails(record),
        });

        if (this.permission.isGranted('Pages.ManpowerPlanningManagement.ManpowerPlannings.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.ManpowerPlanningManagement.ManpowerPlannings.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteManpowerPlanning(record),
            });
        }

        return items;
    }

    // Approve and Reject were removed here on purpose. Manpower plans are now decided through the workflow
    // engine, so approving one happens in Self Service > Inbox alongside every other request type -- with a
    // comment thread, a history and an approval chain, none of which these two buttons had.
    //
    // Leaving them would have meant two ways to approve the same plan, which is the divergence this whole
    // design exists to prevent.

    deleteManpowerPlanning(record: ManpowerPlanningDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.employeeFullNameEn),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._service.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
