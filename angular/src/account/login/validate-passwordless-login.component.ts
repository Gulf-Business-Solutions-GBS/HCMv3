import { AfterViewInit, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Subscription } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { timer } from 'rxjs';
import { LoginService } from './login.service';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import {
    AccountServiceProxy,
    PasswordlessAuthenticateModel,
    VerifyPasswordlessLoginCodeInput,
} from '@shared/service-proxies/service-proxies';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { ReCaptchaV3Service, RecaptchaLoaderService, RECAPTCHA_V3_SITE_KEY } from 'ng-recaptcha-2';
import { AppConsts } from '@shared/AppConsts';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './validate-passwordless-login.component.html',
    styleUrls: ['./validate-passwordless-login.component.less'],
    animations: [accountModuleAnimation()],
    providers: [
        ReCaptchaV3WrapperService,
        ReCaptchaV3Service,
        RecaptchaLoaderService,
        { provide: RECAPTCHA_V3_SITE_KEY, useValue: AppConsts.recaptchaSiteKey },
    ],
    imports: [FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
})
export class ValidatePasswordlessLoginComponent extends AppComponentBase implements OnInit, OnDestroy, AfterViewInit {
    private _router = inject(Router);
    private _accountService = inject(AccountServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    private _activatedRoute = inject(ActivatedRoute);

    loginService = inject(LoginService);

    readonly submitting = signal(false);
    readonly remainingSeconds = signal(90);
    timerSubscription: Subscription;

    readonly selectedProviderInputValue = signal<string>('');
    selectedProvider: string;
    readonly passwordlessCode = signal<string>('');

    ngOnInit(): void {
        this.remainingSeconds.set(this.appSession.application.passwordlessLoginCodeExpireSeconds);

        const timerSource = timer(1000, 1000);
        this.timerSubscription = timerSource.subscribe(() => {
            this.remainingSeconds.update((v) => v - 1);
            if (this.remainingSeconds() === 0) {
                this.message.warn(this.l('TimeoutPleaseTryAgain')).then(() => {
                    this._router.navigate(['account/login']);
                });
            }
        });

        this._activatedRoute.queryParams.subscribe((params) => {
            this.selectedProviderInputValue.set(params['selectedProviderInputValue']);
            this.selectedProvider = params['selectedProvider'];
        });
    }

    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
    }

    ngOnDestroy(): void {
        if (this.timerSubscription) {
            this.timerSubscription.unsubscribe();
            this.timerSubscription = null;
        }
    }

    submit(): void {
        let model = new VerifyPasswordlessLoginCodeInput();
        model.providerValue = this.selectedProviderInputValue();
        model.code = this.passwordlessCode();

        this._accountService.verifyPasswordlessLoginCode(model).subscribe(() => {
            let recaptchaCallback = (token: string) => {
                this.showMainSpinner();
                this.submitting.set(true);

                const passwordlessAuthenticateModel = new PasswordlessAuthenticateModel();

                passwordlessAuthenticateModel.providerValue = this.selectedProviderInputValue();
                passwordlessAuthenticateModel.provider = this.selectedProvider;
                passwordlessAuthenticateModel.verificationCode = this.passwordlessCode();
                passwordlessAuthenticateModel.singleSignIn = UrlHelper.getSingleSignIn();
                passwordlessAuthenticateModel.captchaResponse = token;

                this.loginService.passwordlessAuthenticate(
                    () => {
                        this.submitting.set(false);
                        this.hideMainSpinner();
                    },
                    passwordlessAuthenticateModel,
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
        });
    }
}
