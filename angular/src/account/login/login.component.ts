import { AbpSessionService } from 'abp-ng2-module';
import { AfterViewInit, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SessionServiceProxy, UpdateUserSignInTokenOutput } from '@shared/service-proxies/service-proxies';
import { UrlHelper } from 'shared/helpers/UrlHelper';
import { ExternalLoginProvider, LoginService } from './login.service';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { PasswordMeterComponent } from '@metronic/app/kt/components';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ReCaptchaV3Service, RecaptchaLoaderService, RECAPTCHA_V3_SITE_KEY } from 'ng-recaptcha-2';
import { AppConsts } from '@shared/AppConsts';

@Component({
    templateUrl: './login.component.html',
    animations: [accountModuleAnimation()],
    styleUrls: ['./login.component.less'],
    imports: [FormsModule, AutoFocusDirective, ValidationMessagesComponent, RouterLink, LocalizePipe],
    providers: [
        ReCaptchaV3WrapperService,
        ReCaptchaV3Service,
        RecaptchaLoaderService,
        { provide: RECAPTCHA_V3_SITE_KEY, useValue: AppConsts.recaptchaSiteKey },
    ],
})
export class LoginComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _sessionService = inject(AbpSessionService);
    private _sessionAppService = inject(SessionServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);

    loginService = inject(LoginService);

    readonly submitting = signal(false);
    isMultiTenancyEnabled: boolean = this.multiTenancy.isEnabled;
    readonly capsLockOn = signal(false);
    readonly passwordFocused = signal(false);

    get multiTenancySideIsTenant(): boolean {
        return this._sessionService.tenantId > 0;
    }

    get isTenantSelfRegistrationAllowed(): boolean {
        return this.setting.getBoolean('App.TenantManagement.AllowSelfRegistration');
    }

    get isSelfRegistrationAllowed(): boolean {
        if (!this._sessionService.tenantId) {
            return false;
        }

        return this.setting.getBoolean('App.UserManagement.AllowSelfRegistration');
    }

    get isPasswordlessLoginEnabled(): boolean {
        return (
            this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled') ||
            this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled')
        );
    }

    ngOnInit(): void {
        if (this._sessionService.userId > 0 && UrlHelper.getReturnUrl() && UrlHelper.getSingleSignIn()) {
            this._sessionAppService.updateUserSignInToken().subscribe((result: UpdateUserSignInTokenOutput) => {
                const initialReturnUrl = UrlHelper.getReturnUrl();
                const returnUrl =
                    initialReturnUrl +
                    (initialReturnUrl.indexOf('?') >= 0 ? '&' : '?') +
                    'accessToken=' +
                    result.signInToken +
                    '&userId=' +
                    result.encodedUserId +
                    '&tenantId=' +
                    result.encodedTenantId;

                location.href = returnUrl;
            });
        }

        PasswordMeterComponent.bootstrap();
        this.handleExternalLoginCallbacks();
    }

    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
    }

    handleExternalLoginCallbacks(): void {
        let state = UrlHelper.getQueryParametersUsingHash().state;
        let queryParameters = UrlHelper.getQueryParameters();

        if (state && state.indexOf('openIdConnect') >= 0) {
            this.loginService.openIdConnectLoginCallback({});
        }

        if (queryParameters.state && queryParameters.state.indexOf('openIdConnect') >= 0) {
            this.loginService.openIdConnectLoginCallback({});
        }

        if (queryParameters.twitter && queryParameters.twitter === '1') {
            let parameters = UrlHelper.getQueryParameters();
            let token = parameters['oauth_token'];
            let verifier = parameters['oauth_verifier'];
            this.loginService.twitterLoginCallback(token, verifier);
        }
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

    externalLogin(provider: ExternalLoginProvider) {
        this.loginService.externalAuthenticate(provider);
    }

    checkCapsLock(event: KeyboardEvent) {
        this.capsLockOn.set(event.getModifierState?.('CapsLock') ?? false);
    }
}
