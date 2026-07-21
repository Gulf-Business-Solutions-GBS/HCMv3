import { Component, inject, output, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UserDelegationServiceProxy, UserDelegationDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { CreateNewUserDelegationModalComponent } from './create-new-user-delegation-modal.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';

import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'userDelegationsModal',
    templateUrl: './user-delegations-modal.component.html',
    imports: [
        AppBsModalDirective,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        CreateNewUserDelegationModalComponent,
        LuxonFormatPipe,
        LocalizePipe,
    ],
})
export class UserDelegationsModalComponent extends AppComponentBase {
    private _userDelegationService = inject(UserDelegationServiceProxy);

    readonly modal = viewChild<ModalDirective>('userDelegationsModal');
    readonly createNewUserDelegation = viewChild<CreateNewUserDelegationModalComponent>('createNewUserDelegation');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly modalClose = output<any>();

    getUserDelegations(event?: LazyLoadEvent) {
        this.primengTableHelper.showLoadingIndicator();

        this._userDelegationService
            .getDelegatedUsers(
                this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getSorting(this.dataTable())
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    deleteUserDelegation(userDelegation: UserDelegationDto): void {
        this.message.confirm(
            this.l('UserDelegationDeleteWarningMessage', userDelegation.username),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._userDelegationService.removeDelegation(userDelegation.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }

    reloadPage(): void {
        this.paginator().changePage(this.paginator().getPage());
    }

    manageUserDelegations(): void {
        this.createNewUserDelegation().show();
    }

    show(): void {
        this.modal().show();
    }

    onShown(): void {
        this.getUserDelegations(null);
    }

    close(): void {
        this.modal().hide();
        this.modalClose.emit(null);
    }
}
