import { AfterViewInit, Component, inject, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy } from '@shared/service-proxies/service-proxies';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { LoginService } from './login.service';
import { AppConsts } from '@shared/AppConsts';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { ReCaptchaV3Service, RecaptchaLoaderService, RECAPTCHA_V3_SITE_KEY } from 'ng-recaptcha-2';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-session-lock-screen',
    templateUrl: './session-lock-screen.component.html',
    styleUrls: ['session-lock-screen.component.less'],
    animations: [accountModuleAnimation()],
    providers: [
        ReCaptchaV3WrapperService,
        ReCaptchaV3Service,
        RecaptchaLoaderService,
        { provide: RECAPTCHA_V3_SITE_KEY, useValue: AppConsts.recaptchaSiteKey },
    ],
    imports: [FormsModule, ValidationMessagesComponent, LocalizePipe],
})
export class SessionLockScreenComponent extends AppComponentBase implements AfterViewInit {
    private _profileService = inject(ProfileServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);

    loginService = inject(LoginService);

    readonly userInfo = signal<any>(null);
    readonly submitting = signal(false);
    readonly isExternalLoginEnabled = signal(false);
    readonly externalLoginProviderName = signal<string>('');

    constructor() {
        super();
        this.getLastUserInfo();
    }

    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
    }

    getLastUserInfo(): void {
        let cookie = abp.utils.getCookieValue('userInfo');
        if (!cookie) {
            location.href = '';
        }

        let userInfo = JSON.parse(cookie);
        if (!userInfo) {
            location.href = '';
        }

        this.loginService.authenticateModel.userNameOrEmailAddress = userInfo.userName;
        this.userInfo.set({
            userName: userInfo.userName,
            tenant: userInfo.tenant,
            profilePicture: '',
            externalLoginProviderName: userInfo.externalLoginProviderName,
        });

        this.setIsExternalLoginEnabled(userInfo.externalLoginProviderName);
        this.externalLoginProviderName.set(userInfo.externalLoginProviderName);

        this._profileService.getProfilePictureByUserName(userInfo.userName).subscribe(
            (data) => {
                if (data.profilePicture) {
                    this.userInfo.update((u) => ({
                        ...u,
                        profilePicture: 'data:image/jpeg;base64,' + data.profilePicture,
                    }));
                } else {
                    this.userInfo.update((u) => ({
                        ...u,
                        profilePicture: AppConsts.appBaseUrl + '/assets/common/images/default-profile-picture.png',
                    }));
                }
            },
            () => {
                this.userInfo.update((u) => ({
                    ...u,
                    profilePicture: AppConsts.appBaseUrl + '/assets/common/images/default-profile-picture.png',
                }));
            }
        );
    }

    login(): void {
        let recaptchaCallback = (token: string) => {
            this.showMainSpinner();

            this.submitting.set(true);
            this.loginService.authenticate(
                () => {
                    this.submitting.set(false);
                    this.hideMainSpinner();
                },
                null,
                token
            );
        };

        if (this._recaptchaWrapperService.useCaptchaOnLogin()) {
            this._recaptchaWrapperService
                .getService()
                .execute('login')
                .subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }

    externalLogin(): void {
        const foundProvider = this.loginService.externalLoginProviders.find(
            (p) => p.name === this.externalLoginProviderName()
        );

        this.loginService.externalAuthenticate(foundProvider);
    }

    setIsExternalLoginEnabled(externalLoginProviderName: string): void {
        this.isExternalLoginEnabled.set(externalLoginProviderName != null);
    }
}
