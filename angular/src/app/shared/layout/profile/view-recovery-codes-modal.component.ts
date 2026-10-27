import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ProfileServiceProxy,
    UpdateGoogleAuthenticatorKeyOutput,
    VerifyAuthenticatorCodeInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { RecoveryCodesComponent } from './recovery-codes.component';
import { VerifyCodeModalComponent } from './verify-code-modal.component';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'viewRecoveryCodesModal',
    templateUrl: './view-recovery-codes-modal.component.html',
    imports: [AppBsModalDirective, RecoveryCodesComponent, VerifyCodeModalComponent, LocalizePipe],
})
export class ViewRecoveryCodesModalComponent extends AppComponentBase {
    private _profileService = inject(ProfileServiceProxy);

    readonly modal = viewChild<ModalDirective>('viewRecoveryCodesModal');
    readonly verifyCodeModal = viewChild<VerifyCodeModalComponent>('verifyCodeModal');
    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly recoveryCodesModel = signal<UpdateGoogleAuthenticatorKeyOutput>(new UpdateGoogleAuthenticatorKeyOutput());

    show(): void {
        this.verifyCodeModal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    showRecoveryCodes(verifyCodeInput: VerifyAuthenticatorCodeInput): void {
        this.saving.set(true);
        this._profileService
            .viewRecoveryCodes(verifyCodeInput)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe((result: UpdateGoogleAuthenticatorKeyOutput) => {
                this.recoveryCodesModel.set(result);
                this.modal().show();
                this.modalSave.emit(null);
            });
    }
}
