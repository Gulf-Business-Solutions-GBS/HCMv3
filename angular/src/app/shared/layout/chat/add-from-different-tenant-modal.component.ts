import { Component, OnInit, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateFriendshipWithDifferentTenantInput,
    FriendshipServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'addFromDifferentTenantModal',
    templateUrl: './add-from-different-tenant-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, LocalizePipe],
})
export class AddFromDifferentTenantModalComponent extends AppComponentBase implements OnInit {
    private _friendshipService = inject(FriendshipServiceProxy);

    readonly modal = viewChild<ModalDirective>('addFromDifferentTenantModal');
    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly isHostUser = signal(false);
    readonly tenantToHostChatAllowed = signal(false);
    readonly tenantToTenantChatAllowed = signal(false);
    readonly tenancyName = signal('');
    readonly userName = signal('');

    ngOnInit(): void {
        this.tenantToHostChatAllowed.set(
            this.feature.isEnabled('App.ChatFeature.TenantToHost') && this.appSession.tenantId != null
        );
        this.tenantToTenantChatAllowed.set(
            this.feature.isEnabled('App.ChatFeature.TenantToTenant') || this.appSession.tenantId == null
        );

        this.isHostUser.set(!this.tenantToTenantChatAllowed());
    }

    show(): void {
        this.active.set(true);
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    save(): void {
        let input = new CreateFriendshipWithDifferentTenantInput();
        input.tenancyName = this.tenancyName();
        input.userName = this.userName();

        this.saving.set(true);
        this._friendshipService
            .createFriendshipWithDifferentTenant(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('FriendshipRequestAccepted'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    switchTenant(): void {
        this.tenancyName.set('');
    }
}
