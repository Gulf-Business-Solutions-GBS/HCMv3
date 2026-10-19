import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationStatusDto,
    ApplicationStatusesServiceProxy,
    GetAllApplicationStatusesInput,
    GetAllVacanciesInput,
    VacanciesServiceProxy,
    VacancyDto,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditApplicationStatusModalComponent } from './create-or-edit-application-status-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { Router, RouterLink } from '@angular/router';

@Component({
    templateUrl: './application-statuses.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditApplicationStatusModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        RouterLink,
    ],
})
export class ApplicationStatusesComponent extends AppComponentBase implements OnInit {
    private _service = inject(ApplicationStatusesServiceProxy);
    private _router = inject(Router);
    private _vacanciesService = inject(VacanciesServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditApplicationStatusModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly vacancyIdFilter = signal<number | undefined>(undefined);
    readonly vacancies = signal<VacancyDto[]>([]);
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        this._vacanciesService
            .getAll(new GetAllVacanciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.vacancies.set(result.items));
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllApplicationStatusesInput({
                    vacancyId: this.vacancyIdFilter(),
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
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: ApplicationStatusDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: ApplicationStatusDto): MenuItem[] {
        const items: MenuItem[] = [];

        items.push({
            label: this.l('Details'),
            icon: 'pi pi-list',
            command: () => this._router.navigate(['/app/admin/application-statuses', record.id]),
        });

        if (this.permission.isGranted('Pages.Recruitment.ApplicationStatuses.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.Recruitment.ApplicationStatuses.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: ApplicationStatusDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
