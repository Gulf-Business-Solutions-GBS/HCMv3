import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    TimeEventDto,
    TimeEventsServiceProxy,
    GetAllTimeEventsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditTimeEventModalComponent } from './create-or-edit-time-event-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './time-events.component.html',
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
        CreateOrEditTimeEventModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class TimeEventsComponent extends AppComponentBase implements OnInit {
    private _timeEventsService = inject(TimeEventsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditTimeEventModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getTimeEvents().
    }

    getTimeEvents(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._timeEventsService
            .getAll(
                new GetAllTimeEventsInput({
                    employeeId: undefined,
                    transDateFrom: undefined,
                    transDateTo: undefined,
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
        this.getTimeEvents({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createTimeEvent(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: TimeEventDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: TimeEventDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.TimeManagement.TimeEvents.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.TimeManagement.TimeEvents.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteTimeEvent(record),
            });
        }

        // Geo punches taken with a selfie: lets HR check who actually punched.
        if (record.geoSelfiePath) {
            items.push({
                label: this.l('GeoPunchViewSelfie'),
                icon: 'pi pi-image',
                command: () => this.viewSelfie(record),
            });
        }

        return items;
    }

    /** The selfie being previewed, as a data URL; null when the preview is closed. */
    readonly selfiePreview = signal<string | null>(null);

    viewSelfie(record: TimeEventDto): void {
        this._timeEventsService.getTimeEventSelfie(record.id).subscribe((result) => {
            if (result.dataUrl) {
                this.selfiePreview.set(result.dataUrl);
            } else {
                this.notify.warn(this.l('AttachmentFileMissing'));
            }
        });
    }

    deleteTimeEvent(record: TimeEventDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.employeeFullNameEn),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._timeEventsService.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
