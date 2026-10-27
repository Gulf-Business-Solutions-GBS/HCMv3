import { Component, OnInit, inject, output, viewChild, signal } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CurrentUserProfileEditDto,
    SettingScopes,
    ProfileServiceProxy,
    SendVerificationSmsInputDto,
    GenerateGoogleAuthenticatorKeyOutput,
    VerifyAuthenticatorCodeInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { SmsVerificationModalComponent } from './sms-verification-modal.component';
import { finalize } from 'rxjs/operators';
import { EnableTwoFactorAuthenticationModalComponent } from './enable-two-factor-authentication-modal.component';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { Tooltip } from 'primeng/tooltip';
import { TimeZoneComboComponent } from '../../common/timing/timezone-combo.component';
import { ViewRecoveryCodesModalComponent } from './view-recovery-codes-modal.component';
import { VerifyCodeModalComponent } from './verify-code-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'mySettingsModal',
    templateUrl: './my-settings-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        Tooltip,
        TimeZoneComboComponent,
        SmsVerificationModalComponent,
        EnableTwoFactorAuthenticationModalComponent,
        ViewRecoveryCodesModalComponent,
        VerifyCodeModalComponent,
        LocalizePipe,
    ],
})
export class MySettingsModalComponent extends AppComponentBase implements OnInit {
    private _profileService = inject(ProfileServiceProxy);
    private _initialTimezone: string = undefined;

    readonly modal = viewChild<ModalDirective>('mySettingsModal');
    readonly enableTwoFactorAuthenticationModal =
        viewChild<EnableTwoFactorAuthenticationModalComponent>('enableTwoFactor');
    readonly smsVerificationModal = viewChild<SmsVerificationModalComponent>('smsVerificationModal');
    readonly verifyCodeModal = viewChild<SmsVerificationModalComponent>('verifyCodeModal');
    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly isGoogleAuthenticatorEnabled = signal(false);
    readonly isPhoneNumberConfirmed = signal<boolean>(false);
    readonly smsEnabled = signal(false);
    readonly user = signal<CurrentUserProfileEditDto>(undefined);
    readonly showTimezoneSelection: boolean = abp.clock.provider.supportsMultipleTimezone;
    readonly canChangeUserName = signal(false);
    readonly defaultTimezoneScope: SettingScopes = SettingScopes.User;
    readonly savedPhoneNumber = signal<string>(undefined);
    readonly newPhoneNumber = signal<string>(undefined);

    readonly isMultiTenancyEnabled = signal(this.multiTenancy.isEnabled);
    readonly isTwoFactorLoginEnabledForApplication = signal(false);

    ngOnInit(): void {
        this.isTwoFactorLoginEnabledForApplication.set(
            abp.setting.getBoolean('Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled')
        );
    }

    show(): void {
        this.active.set(true);
        this._profileService.getCurrentUserProfileForEdit().subscribe((result) => {
            this.smsEnabled.set(this.setting.getBoolean('App.UserManagement.SmsVerificationEnabled'));
            this.user.set(result);
            this._initialTimezone = result.timezone;
            this.canChangeUserName.set(result.userName !== AppConsts.userManagement.defaultAdminUserName);
            this.modal().show();
            this.isGoogleAuthenticatorEnabled.set(result.isGoogleAuthenticatorEnabled);
            this.isPhoneNumberConfirmed.set(result.isPhoneNumberConfirmed);
            this.savedPhoneNumber.set(result.phoneNumber);
        });
    }

    enableTwoFactorAuthentication(): void {
        this._profileService
            .generateGoogleAuthenticatorKey()
            .subscribe((result: GenerateGoogleAuthenticatorKeyOutput) => {
                this.enableTwoFactorAuthenticationModal().model.set(result);
            });
        this.enableTwoFactorAuthenticationModal().show();
    }

    disableTwoFactorAuthentication(verifyCodeInput: VerifyAuthenticatorCodeInput): void {
        this._profileService
            .disableGoogleAuthenticator(verifyCodeInput)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                this.close();
                this.message.success(this.l('TwoFactorAuthenticationDisabled'));
            });
    }

    smsVerify(): void {
        let input = new SendVerificationSmsInputDto();
        input.phoneNumber = this.user().phoneNumber;
        this._profileService.sendVerificationSms(input).subscribe(() => {
            this.smsVerificationModal().show();
        });
    }

    changePhoneNumberToVerified(): void {
        this.isPhoneNumberConfirmed.set(true);
        this.savedPhoneNumber.set(this.user().phoneNumber);
    }

    onShown(): void {
        document.getElementById('Name').focus();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    save(): void {
        this.saving.set(true);
        const currentUser = this.user();
        this._profileService
            .updateCurrentUserProfile(currentUser)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                if (this.appSession.user.emailAddress !== currentUser.emailAddress) {
                    this.notify.info(this.l('ChangeEmailRequestSentMessage'));
                } else {
                    this.notify.info(this.l('SavedSuccessfully'));
                }

                this.appSession.user.name = currentUser.name;
                this.appSession.user.surname = currentUser.surname;
                this.appSession.user.userName = currentUser.userName;
                this.appSession.user.emailAddress = currentUser.emailAddress;

                this.close();
                this.modalSave.emit(null);

                if (abp.clock.provider.supportsMultipleTimezone && this._initialTimezone !== currentUser.timezone) {
                    this.message.info(this.l('TimeZoneSettingChangedRefreshPageNotification')).then(() => {
                        window.location.reload();
                    });
                }
            });
    }
}
