import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LinkToUserInput, UserLinkServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'linkAccountModal',
    templateUrl: './link-account-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class LinkAccountModalComponent extends AppComponentBase {
    private _userLinkService = inject(UserLinkServiceProxy);

    readonly modal = viewChild<ModalDirective>('linkAccountModal');

    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);

    linkUser: LinkToUserInput = new LinkToUserInput();

    show(): void {
        this.active.set(true);
        this.linkUser = new LinkToUserInput();
        this.linkUser.tenancyName = this.appSession.tenancyName;
        this.modal().show();
    }

    onShown(): void {
        document.getElementById('TenancyName').focus();
    }

    save(): void {
        this.saving.set(true);
        this._userLinkService
            .linkToUser(this.linkUser)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
