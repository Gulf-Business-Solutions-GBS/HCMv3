import { AbpMultiTenancyService } from 'abp-ng2-module';
import { Component, inject, output, viewChild } from '@angular/core';
import { LinkedAccountService } from '@app/shared/layout/linked-account.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LinkedUserDto, UnlinkUserInput, UserLinkServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { LinkAccountModalComponent } from './link-account-modal.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';

import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'linkedAccountsModal',
    templateUrl: './linked-accounts-modal.component.html',
    imports: [
        AppBsModalDirective,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        LinkAccountModalComponent,
        LocalizePipe,
    ],
})
export class LinkedAccountsModalComponent extends AppComponentBase {
    private abpMultiTenancyService = inject(AbpMultiTenancyService);
    private _userLinkService = inject(UserLinkServiceProxy);
    private _linkedAccountService = inject(LinkedAccountService);

    readonly modal = viewChild<ModalDirective>('linkedAccountsModal');
    readonly linkAccountModal = viewChild<LinkAccountModalComponent>('linkAccountModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly modalClose = output<any>();

    getLinkedUsers(event?: LazyLoadEvent) {
        this.primengTableHelper.showLoadingIndicator();

        this._userLinkService
            .getLinkedUsers(
                this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getSorting(this.dataTable())
            )
            .pipe(
                finalize(() => {
                    this.primengTableHelper.hideLoadingIndicator();
                })
            )
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    getShownLinkedUserName(linkedUser: LinkedUserDto): string {
        if (!this.abpMultiTenancyService.isEnabled) {
            return linkedUser.username;
        }

        return (linkedUser.tenantId ? linkedUser.tenancyName : '.') + '\\' + linkedUser.username;
    }

    deleteLinkedUser(linkedUser: LinkedUserDto): void {
        this.message.confirm(
            this.l('LinkedUserDeleteWarningMessage', linkedUser.username),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    const unlinkUserInput = new UnlinkUserInput();
                    unlinkUserInput.userId = linkedUser.id;
                    unlinkUserInput.tenantId = linkedUser.tenantId;

                    this._userLinkService.unlinkUser(unlinkUserInput).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyUnlinked'));
                    });
                }
            }
        );
    }

    reloadPage(): void {
        this.paginator().changePage(this.paginator().getPage());
    }

    manageLinkedAccounts(): void {
        this.linkAccountModal().show();
    }

    switchToUser(linkedUser: LinkedUserDto): void {
        this._linkedAccountService.switchToAccount(linkedUser.id, linkedUser.tenantId);
    }

    show(): void {
        this.modal().show();
    }

    close(): void {
        this.modal().hide();
        this.modalClose.emit(null);
    }
}
