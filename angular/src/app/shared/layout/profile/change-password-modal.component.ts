import { Component, ElementRef, inject, viewChild, signal } from '@angular/core';
import { PasswordMeterComponent } from '@metronic/app/kt/components';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ChangePasswordInput,
    PasswordComplexitySetting,
    ProfileServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { EqualValidator } from '../../../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../../../shared/utils/validation/password-complexity-validator.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'changePasswordModal',
    templateUrl: './change-password-modal.component.html',
    styleUrls: ['./change-password-modal.component.less'],
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        EqualValidator,
        PasswordComplexityValidator,
        LocalizePipe,
    ],
})
export class ChangePasswordModalComponent extends AppComponentBase {
    private _profileService = inject(ProfileServiceProxy);

    readonly currentPasswordInput = viewChild<ElementRef>('currentPasswordInput');
    readonly modal = viewChild<ModalDirective>('changePasswordModal');

    readonly passwordComplexitySetting = signal<PasswordComplexitySetting>(new PasswordComplexitySetting());
    readonly currentPassword = signal<string>('');
    readonly password = signal<string>('');
    readonly confirmPassword = signal<string>('');

    readonly saving = signal(false);
    readonly active = signal(false);

    show(): void {
        this.active.set(true);
        this.currentPassword.set('');
        this.password.set('');
        this.confirmPassword.set('');

        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting.set(result.setting);
            this.modal().show();
        });
    }

    onShown(): void {
        PasswordMeterComponent.bootstrap();
        document.getElementById('CurrentPassword').focus();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    save(): void {
        let input = new ChangePasswordInput();
        input.currentPassword = this.currentPassword();
        input.newPassword = this.password();

        this.saving.set(true);
        this._profileService
            .changePassword(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('YourPasswordHasChangedSuccessfully'));
                this.close();
            });
    }
}
