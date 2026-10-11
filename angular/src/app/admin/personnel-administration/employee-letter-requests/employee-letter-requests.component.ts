import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeLetterRequestDto,
    EmployeeLetterRequestsServiceProxy,
    EntityDto,
    GetAllEmployeeLetterRequestsInput,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * HR's list of letter requests, and where an approved one is printed (legacy: the Print Letter action on the
 * Letter Request view). Printing produces the certificate for the request's document type, opens it, and
 * keeps the PDF on the request, where it is listed under Attachments.
 */
@Component({
    templateUrl: './employee-letter-requests.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, TableModule, PaginatorModule, TieredMenu, Tooltip, AttachmentsComponent, LocalizePipe],
})
export class EmployeeLetterRequestsComponent extends AppComponentBase implements OnInit {
    private _letterRequestsService = inject(EmployeeLetterRequestsServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    /** The request being printed, so its row cannot be printed twice at once. */
    readonly printingId = signal<number | null>(null);

    /** The row whose attachments are expanded in the list, if any. */
    readonly expandedId = signal<number | null>(null);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getLetterRequests().
    }

    getLetterRequests(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._letterRequestsService
            .getAll(
                new GetAllEmployeeLetterRequestsInput({
                    employeeId: undefined,
                    documentTypeId: undefined,

                    // HR sees what is still on its way as well; only an approved one can be printed.
                    includePending: true,
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getLetterRequests({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    showMenu(event: Event, record: EmployeeLetterRequestDto, menu: TieredMenu): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    /** Every action on every row; one that does not apply is greyed out rather than left out. */
    getMenuItems(record: EmployeeLetterRequestDto): MenuItem[] {
        return [
            {
                label: this.l('PrintLetter'),
                icon: 'pi pi-print',
                disabled:
                    !record.canPrint ||
                    this.printingId() !== null ||
                    !this.permission.isGranted('Pages.PersonnelAdministration.EmployeeLetterRequests.Print'),
                command: () => this.printLetter(record),
            },
            {
                label: this.l('Attachments'),
                icon: 'pi pi-paperclip',
                command: () => this.expandedId.set(this.expandedId() === record.id ? null : record.id),
            },
        ];
    }

    /**
     * Prints the letter and opens it. The tab is opened here, inside the click, and pointed at the PDF once
     * it arrives: a tab opened after the server has answered is what a popup blocker stops.
     */
    printLetter(record: EmployeeLetterRequestDto): void {
        if (this.printingId() !== null) {
            return;
        }

        const tab = window.open('', '_blank');

        this.printingId.set(record.id);

        this._letterRequestsService
            .printLetter(new EntityDto({ id: record.id }))
            .pipe(finalize(() => this.printingId.set(null)))
            .subscribe({
                next: (printed) => {
                    const blob = this.pdfBlob(printed.base64Content ?? '');
                    const url = URL.createObjectURL(blob);

                    if (tab) {
                        tab.location.href = url;
                    } else {
                        // The browser refused the tab; the file is handed over as a download instead.
                        const link = document.createElement('a');
                        link.href = url;
                        link.download = printed.fileName ?? 'Letter.pdf';
                        link.click();
                    }

                    if (printed.savedOnRequest) {
                        this.notify.success(this.l('LetterPrintedAndSavedOnRequest'));
                    } else {
                        this.message.warn(printed.notSavedReason ?? '', this.l('LetterPrintedButNotSavedOnRequest'));
                    }

                    // Shows the copy just kept, and refreshes it if the row was already open.
                    this.expandedId.set(null);
                    setTimeout(() => this.expandedId.set(record.id));
                },
                error: () => tab?.close(),
            });
    }

    private pdfBlob(base64: string): Blob {
        const bytes = atob(base64);
        const buffer = new Uint8Array(bytes.length);

        for (let i = 0; i < bytes.length; i++) {
            buffer[i] = bytes.charCodeAt(i);
        }

        return new Blob([buffer], { type: 'application/pdf' });
    }

    /** A letter with no workflow status was recorded by HR rather than requested. */
    statusLabel(record: EmployeeLetterRequestDto): string {
        if (record.workflowStatus === undefined || record.workflowStatus === null) {
            return this.l('Recorded');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[record.workflowStatus]);
    }

    statusClass(record: EmployeeLetterRequestDto): string {
        switch (record.workflowStatus) {
            case RequestStatus.Open:
                return 'badge-warning';
            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return 'badge-success';
            case RequestStatus.Rejected:
                return 'badge-danger';
            case RequestStatus.Draft:
                return 'badge-light-warning';
            default:
                return 'badge-secondary';
        }
    }

    typeName(record: EmployeeLetterRequestDto): string {
        const rtl = document?.documentElement?.getAttribute('dir') === 'rtl';

        return rtl ? record.documentTypeNameAr || record.documentTypeNameEn || '' : record.documentTypeNameEn || '';
    }

    date(value: { toFormat(format: string): string; year?: number } | undefined): string {
        return !value || (value.year !== undefined && value.year <= 1) ? '' : value.toFormat('yyyy-MM-dd');
    }
}
