import { Component, ViewEncapsulation, inject, output, viewChild, signal } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { NameValueDto, NotificationServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../shared/utils/auto-focus.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';

import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'user-lookup-table-modal',
    templateUrl: './user-lookup-table-modal.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        AppBsModalDirective,
        FormsModule,
        AutoFocusDirective,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class UserLookupTableModalComponent extends AppComponentBase {
    private _notificationServiceProxy = inject(NotificationServiceProxy);

    readonly modal = viewChild<ModalDirective>('userLookupTableModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly modalSave = output<NameValueDto[]>();

    readonly filterText = signal('');
    readonly active = signal(false);
    readonly saving = signal(false);

    selectedUsers: NameValueDto[];

    show(): void {
        this.active.set(true);
        this.paginator().rows = 5;
        this.getAll();
        this.modal().show();
    }

    getAll(event?: LazyLoadEvent) {
        if (!this.active()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._notificationServiceProxy
            .getAllUserForLookupTable(
                this.filterText(),
                this.primengTableHelper.getSorting(this.dataTable()),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event)
            )
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        this.paginator().changePage(this.paginator().getPage());
    }

    save() {
        this.active.set(false);
        this.modal().hide();
        this.modalSave.emit(this.selectedUsers);
        this.selectedUsers = [];
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
        this.selectedUsers = [];
    }
}
