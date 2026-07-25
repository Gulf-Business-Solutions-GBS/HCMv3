import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetAllManpowerPlanningDetailsInput,
    GetAllManpowerPlanningsInput,
    ManpowerPlanningDetailDto,
    ManpowerPlanningDetailsServiceProxy,
    ManpowerPlanningDto,
    ManpowerPlanningsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditManpowerPlanningDetailModalComponent } from './create-or-edit-manpower-planning-detail-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    templateUrl: './manpower-planning-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditManpowerPlanningDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class ManpowerPlanningDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _service = inject(ManpowerPlanningDetailsServiceProxy);
    private _manpowerPlanningsService = inject(ManpowerPlanningsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditManpowerPlanningDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);
    readonly manpowerPlanningId = signal<number | undefined>(undefined);
    readonly manpowerPlanning = signal<ManpowerPlanningDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.manpowerPlanningId.set(id);
                this.loadManpowerPlanning(id);
            }
        });
        // Note: the PrimeNG table's (onLazyLoad) triggers the first getManpowerPlanningDetails().
    }

    private loadManpowerPlanning(id: number): void {
        this._manpowerPlanningsService
            .getAll(
                new GetAllManpowerPlanningsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    organizationUnitId: undefined,
                    periodId: undefined,
                    status: undefined as any,
                })
            )
            .subscribe((result) => {
                this.manpowerPlanning.set(result.items.find((item) => item.id === id));
            });
    }

    getManpowerPlanningDetails(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllManpowerPlanningDetailsInput({
                    manpowerPlanningId: this.manpowerPlanningId(),
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
        this.getManpowerPlanningDetails({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createManpowerPlanningDetail(): void {
        this.createOrEditModal().show(undefined, this.manpowerPlanningId());
    }

    showMenu(event: Event, record: ManpowerPlanningDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: ManpowerPlanningDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.ManpowerPlanningManagement.ManpowerPlanningDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.manpowerPlanningId()),
            });
        }

        if (this.permission.isGranted('Pages.ManpowerPlanningManagement.ManpowerPlanningDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteManpowerPlanningDetail(record),
            });
        }

        return items;
    }

    deleteManpowerPlanningDetail(record: ManpowerPlanningDetailDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.jobTitleNameEn),
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
