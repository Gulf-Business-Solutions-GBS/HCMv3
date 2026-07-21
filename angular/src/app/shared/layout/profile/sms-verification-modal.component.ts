import { Component, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy, VerifySmsCodeInputDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'smsVerificationModal',
    templateUrl: './sms-verification-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
})
export class SmsVerificationModalComponent extends AppComponentBase {
    private _profileService = inject(ProfileServiceProxy);

    readonly nameInput = viewChild<ElementRef>('nameInput');
    readonly modal = viewChild<ModalDirective>('smsVerificationModal');
    readonly newPhoneNumber = input<string>(undefined);
    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly verifyCodeInput = signal<VerifySmsCodeInputDto>(new VerifySmsCodeInputDto());

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
            const updated = new VerifySmsCodeInputDto();
            updated.init({ ...input.toJSON(), code });
            return updated;
        });
    }

    save(): void {
        this.saving.set(true);
        this.verifyCodeInput.update((input) => {
            const updated = new VerifySmsCodeInputDto();
            updated.init({ ...input.toJSON(), phoneNumber: this.newPhoneNumber() });
            return updated;
        });
        this._profileService
            .verifySmsCode(this.verifyCodeInput())
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                this.close();
                this.modalSave.emit(null);
            });
    }
}
