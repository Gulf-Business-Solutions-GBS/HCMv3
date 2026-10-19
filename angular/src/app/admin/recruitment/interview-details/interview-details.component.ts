import { Component, OnInit, computed, inject, input, viewChild, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationStatusDto,
    ApplicationStatusesServiceProxy,
    GetAllApplicationStatusesInput,
    GetAllInterviewDetailsInput,
    InterviewDetailsDto,
    InterviewDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditInterviewDetailModalComponent } from './create-or-edit-interview-detail-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

@Component({
    selector: 'interviewDetails',
    templateUrl: './interview-details.component.html',
    animations: [appModuleAnimation()],
    imports: [
        NgTemplateOutlet,
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditInterviewDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class InterviewDetailsComponent extends AppComponentBase implements OnInit {
    private _service = inject(InterviewDetailsServiceProxy);
    private _applicationStatusesService = inject(ApplicationStatusesServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditInterviewDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /**
     * Set when embedded in an application's page (its Interview Details tab): the list shows only that
     * application's interviews, without the page header or the application filter. Unset on the route.
     */
    readonly applicationStatusId = input<number | undefined>(undefined);
    readonly embedded = computed(() => !!this.applicationStatusId());

    readonly applicationStatusIdFilter = signal<number | undefined>(undefined);
    readonly applicationStatuses = signal<ApplicationStatusDto[]>([]);
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        if (this.embedded()) {
            return;
        }
        this._applicationStatusesService
            .getAll(new GetAllApplicationStatusesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, vacancyId: undefined }))
            .subscribe((result) => this.applicationStatuses.set(result.items));
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllInterviewDetailsInput({
                    applicationStatusId: this.applicationStatusId() ?? this.applicationStatusIdFilter(),
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
        this.createOrEditModal().show(undefined, this.applicationStatusId());
    }

    showMenu(event: Event, record: InterviewDetailsDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: InterviewDetailsDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Recruitment.InterviewDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.applicationStatusId()),
            });
        }

        if (this.permission.isGranted('Pages.Recruitment.InterviewDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: InterviewDetailsDto): void {
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
