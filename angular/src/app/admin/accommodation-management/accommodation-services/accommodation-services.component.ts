import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccommodationServicesDto,
    AccommodationServicesServiceProxy,
    EmployeeAccommodationDto,
    EmployeeAccommodationsServiceProxy,
    GetAllAccommodationServicesInput,
    GetAllEmployeeAccommodationsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditAccommodationServiceModalComponent } from './create-or-edit-accommodation-service-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './accommodation-services.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditAccommodationServiceModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class AccommodationServicesComponent extends AppComponentBase implements OnInit {
    private _service = inject(AccommodationServicesServiceProxy);
    private _employeeAccommodationsService = inject(EmployeeAccommodationsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditAccommodationServiceModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);
    readonly employeeAccommodations = signal<EmployeeAccommodationDto[]>([]);

    ngOnInit(): void {
        this._employeeAccommodationsService
            .getAll(new GetAllEmployeeAccommodationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, employeeId: undefined }))
            .subscribe((result) => this.employeeAccommodations.set(result.items));
        // Intentionally not calling getAccommodationServices() here: the PrimeNG table's
        // (onLazyLoad) triggers the first load.
    }

    employeeAccommodationLabel(employeeAccommodationId: number): string {
        const match = this.employeeAccommodations().find((ea) => ea.id === employeeAccommodationId);
        return match ? `${match.employeeCode} - ${match.employeeFullNameEn}` : `#${employeeAccommodationId}`;
    }

    getAccommodationServices(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllAccommodationServicesInput({
                    employeeAccommodationId: undefined,
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
        this.getAccommodationServices({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createAccommodationService(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: AccommodationServicesDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: AccommodationServicesDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.AccommodationManagement.AccommodationServices.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.AccommodationManagement.AccommodationServices.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteAccommodationService(record),
            });
        }

        return items;
    }

    deleteAccommodationService(record: AccommodationServicesDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.accommodationServiceTypeName),
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
