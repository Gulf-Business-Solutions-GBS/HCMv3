import { Component, OnInit, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateFriendshipForCurrentTenantInput,
    CreateFriendshipRequestInput,
    FindUsersOutputDto,
    FriendshipServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { merge as _merge } from 'lodash-es';
import { AddFromDifferentTenantModalComponent } from './add-from-different-tenant-modal.component';
import { finalize } from 'rxjs';
import { NgForm, FormsModule } from '@angular/forms';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FriendsLookupTableComponent } from './friends-lookup-table.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'addFriendModal',
    templateUrl: './add-friend-modal.component.html',
    imports: [
        AppBsModalDirective,
        FriendsLookupTableComponent,
        FormsModule,
        AddFromDifferentTenantModalComponent,
        LocalizePipe,
    ],
})
export class AddFriendModalComponent extends AppComponentBase implements OnInit {
    private _friendshipService = inject(FriendshipServiceProxy);

    readonly itemSelected = output<FindUsersOutputDto>();

    readonly modal = viewChild<ModalDirective>('modal');
    readonly verifyCodeModal = viewChild<AddFromDifferentTenantModalComponent>('addFromDifferentTenantModal');

    readonly saving = signal(false);
    readonly tenantId = signal<number | undefined>(undefined);
    readonly interTenantChatAllowed = signal(false);
    readonly canListUsersInTenant = signal(false);
    readonly userName = signal('');

    ngOnInit(): void {
        this.interTenantChatAllowed.set(
            this.feature.isEnabled('App.ChatFeature.TenantToTenant') ||
                this.feature.isEnabled('App.ChatFeature.TenantToHost') ||
                !this.appSession.tenant
        );

        this.canListUsersInTenant.set(this.permission.isGranted('Pages.Administration.Users'));
    }

    addFriendSelected(item: FindUsersOutputDto): void {
        const input = new CreateFriendshipRequestInput();
        input.userId = item.id;
        input.tenantId = this.appSession.tenant ? this.appSession.tenant.id : null;

        this._friendshipService
            .createFriendshipRequest(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('FriendshipRequestAccepted'));
                this.close();
            });
    }

    save(): void {
        let input = new CreateFriendshipForCurrentTenantInput();
        input.userName = this.userName();

        this.saving.set(true);
        this._friendshipService
            .createFriendshipForCurrentTenant(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                    this.userName.set('');
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('FriendshipRequestAccepted'));
            });
    }

    show(): void {
        this.modal().show();
    }

    close(): void {
        this.modal().hide();
    }
}
