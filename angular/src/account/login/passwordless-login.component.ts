import { Component, OnInit, inject, signal } from '@angular/core';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountServiceProxy,
    PasswordlessLoginProviderType,
    SendPasswordlessLoginCodeInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';
import { FormsModule } from '@angular/forms';

import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './passwordless-login.component.html',
    animations: [accountModuleAnimation()],
    imports: [FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
})
export class PasswordlessLoginComponent extends AppComponentBase implements OnInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);

    readonly submitting = signal(false);
    PasswordlessLoginProviderType = PasswordlessLoginProviderType;
    readonly selectedPasswordlessLoginProvider = signal<PasswordlessLoginProviderType>(
        PasswordlessLoginProviderType.Email
    );

    readonly selectedProviderInputValue = signal('');
    readonly passwordlessLoginProviders = signal<{ value: PasswordlessLoginProviderType; text: string }[]>([]);

    get isEmailPasswordlessLoginEnabled(): boolean {
        return this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled');
    }

    get isSmsPasswordlessLoginEnabled(): boolean {
        return this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled');
    }

    ngOnInit(): void {
        this.passwordlessLoginProviders.set(
            Object.keys(PasswordlessLoginProviderType)
                .filter((key) => isNaN(Number(key)))
                .map((value) => ({
                    value: PasswordlessLoginProviderType[value],
                    text: this.getPasswordlessProviderText(PasswordlessLoginProviderType[value]),
                }))
        );

        if (this.isEmailPasswordlessLoginEnabled) {
            this.selectedPasswordlessLoginProvider.set(PasswordlessLoginProviderType.Email);
        } else {
            this.selectedPasswordlessLoginProvider.set(PasswordlessLoginProviderType.Sms);
        }
    }

    submit(): void {
        const model = new SendPasswordlessLoginCodeInput();
        model.providerValue = this.selectedProviderInputValue();
        model.providerType = this.selectedPasswordlessLoginProvider() as PasswordlessLoginProviderType; // Cast the value to 'PasswordlessProviderType'

        this._accountService
            .sendPasswordlessLoginCode(model)
            .pipe(finalize(() => this.submitting.set(false)))
            .subscribe(() => {
                this._router.navigate(['account/verify-passwordless-login'], {
                    queryParams: {
                        selectedProviderInputValue: this.selectedProviderInputValue(),
                        selectedProvider: this.passwordlessLoginProviders().find(
                            (p) => p.value === this.selectedPasswordlessLoginProvider()
                        ).text,
                    },
                });
            });
    }

    getPasswordlessProviderText(providerType: PasswordlessLoginProviderType): string {
        const key = Object.keys(PasswordlessLoginProviderType).find(
            (k) => PasswordlessLoginProviderType[k] === providerType
        );
        return key ? key : '';
    }
}
