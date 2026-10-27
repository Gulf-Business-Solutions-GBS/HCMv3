import { Component, OnInit, inject, output, signal, viewChild } from '@angular/core';
import { StepperComponent } from '@metronic/app/kt/components';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GenerateGoogleAuthenticatorKeyOutput,
    ProfileServiceProxy,
    UpdateGoogleAuthenticatorKeyInput,
    UpdateGoogleAuthenticatorKeyOutput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs';
import { RecoveryCodesComponent } from './recovery-codes.component';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'enableTwoFactorAuthenticationModal',
    templateUrl: './enable-two-factor-authentication-modal.component.html',
    styleUrls: ['enable-two-factor-authentication-modal.component.less'],
    imports: [AppBsModalDirective, FormsModule, RecoveryCodesComponent, LocalizePipe],
})
export class EnableTwoFactorAuthenticationModalComponent extends AppComponentBase implements OnInit {
    private _profileService = inject(ProfileServiceProxy);

    readonly modal = viewChild<ModalDirective>('enableTwoFactorAuthenticationModal');
    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly model = signal<GenerateGoogleAuthenticatorKeyOutput>(new GenerateGoogleAuthenticatorKeyOutput());
    readonly recoveryCodesModel = signal<UpdateGoogleAuthenticatorKeyOutput>(new UpdateGoogleAuthenticatorKeyOutput());

    stepper: StepperComponent;

    ngOnInit(): void {
        this.stepper = StepperComponent.createInsance(document.getElementById('2fa_stepper'));

        this.stepper.on('kt.stepper.next', () => {
            this.stepper.goNext();
        });
    }

    onAuthenticatorCodeInput(event: any): void {
        if (event.target.value.length !== 6) {
            return;
        }

        const btnContinue = document.getElementById('btnContinue');
        btnContinue.setAttribute('data-kt-indicator', 'on');

        let input = new UpdateGoogleAuthenticatorKeyInput();
        input.authenticatorCode = event.target.value;
        input.googleAuthenticatorKey = this.model().googleAuthenticatorKey;

        this.saving.set(true);
        this._profileService
            .updateGoogleAuthenticatorKey(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                    event.target.value = '';
                })
            )
            .subscribe((result: UpdateGoogleAuthenticatorKeyOutput) => {
                this.recoveryCodesModel.set(result);
                this.saving.set(false);
                this.stepper.goNext();
            });

        btnContinue.removeAttribute('data-kt-indicator');
    }

    downloadRecoveryCodes(recoveryCodes: string[]): void {
        const recoveryCodesText = recoveryCodes.join('\r\n');

        const element = document.createElement('a');
        element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(recoveryCodesText));
        element.setAttribute('download', 'recovery-codes.txt');
        element.click();

        URL.revokeObjectURL(element.href);

        const btnContinue = document.getElementById('btnContinue');

        // User can continue
        btnContinue.removeAttribute('disabled');
    }

    copyRecoveryCodes(recoveryCodes: string[]): void {
        const recoveryCodesText = recoveryCodes.join('\r\n');

        navigator.clipboard.writeText(recoveryCodesText);

        const btnContinue = document.getElementById('btnContinue');

        // User can continue
        btnContinue.removeAttribute('disabled');
    }

    show(): void {
        this.active.set(true);
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
        this.message.success(this.l('TwoFactorAuthenticationEnabled'));
        this.modalSave.emit(null);
    }
}
