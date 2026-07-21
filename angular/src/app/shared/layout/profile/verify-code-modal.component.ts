import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy, VerifyAuthenticatorCodeInput } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'verifyCodeModal',
    templateUrl: './verify-code-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
})
export class VerifyCodeModalComponent extends AppComponentBase {
    private _profileService = inject(ProfileServiceProxy);

    readonly modal = viewChild<ModalDirective>('verifyCodeModal');
    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly verifyCodeInput = signal<VerifyAuthenticatorCodeInput>(new VerifyAuthenticatorCodeInput());

    show(): void {
        this.active.set(true);
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateCode(code: string): void {
        this.verifyCodeInput.update((input) => {
            const updated = new VerifyAuthenticatorCodeInput();
            updated.init({ ...input.toJSON(), code });
            return updated;
        });
    }

    save(): void {
        this.saving.set(true);
        this._profileService
            .verifyAuthenticatorCode(this.verifyCodeInput())
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                    this.verifyCodeInput.update((input) => {
                        const updated = new VerifyAuthenticatorCodeInput();
                        updated.init({ ...input.toJSON(), code: '' });
                        return updated;
                    });
                })
            )
            .subscribe(() => {
                this.close();
                this.modalSave.emit(null);
            });
    }
}
