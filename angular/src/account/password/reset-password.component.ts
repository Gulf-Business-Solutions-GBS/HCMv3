import { AfterViewInit, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountServiceProxy,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    ResetPasswordOutput,
    ResolveTenantIdInput,
} from '@shared/service-proxies/service-proxies';
import { LoginService } from '../login/login.service';
import { ResetPasswordModel } from './reset-password.model';
import { finalize } from 'rxjs/operators';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { ReCaptchaV3Service, RecaptchaLoaderService, RECAPTCHA_V3_SITE_KEY } from 'ng-recaptcha-2';
import { AppConsts } from '@shared/AppConsts';
import { FormsModule } from '@angular/forms';
import { EqualValidator } from '../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../shared/utils/validation/password-complexity-validator.directive';

import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './reset-password.component.html',
    animations: [accountModuleAnimation()],
    providers: [
        ReCaptchaV3WrapperService,
        ReCaptchaV3Service,
        RecaptchaLoaderService,
        { provide: RECAPTCHA_V3_SITE_KEY, useValue: AppConsts.recaptchaSiteKey },
    ],
    imports: [FormsModule, EqualValidator, PasswordComplexityValidator, RouterLink, ButtonBusyDirective, LocalizePipe],
})
export class ResetPasswordComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    private _loginService = inject(LoginService);
    private _profileService = inject(ProfileServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);

    model: ResetPasswordModel = new ResetPasswordModel();
    readonly passwordComplexitySetting = signal<PasswordComplexitySetting>(new PasswordComplexitySetting());
    readonly saving = signal(false);

    ngOnInit(): void {
        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting.set(result.setting);
        });

        if (this._activatedRoute.snapshot.queryParams['c']) {
            this.model.c = this._activatedRoute.snapshot.queryParams['c'];

            this._accountService
                .resolveTenantId(new ResolveTenantIdInput({ c: this.model.c }))
                .subscribe((tenantId) => {
                    this.appSession.changeTenantIfNeeded(tenantId);
                });
        } else {
            this.model.userId = this._activatedRoute.snapshot.queryParams['userId'];
            this.model.resetCode = this._activatedRoute.snapshot.queryParams['resetCode'];

            this.appSession.changeTenantIfNeeded(
                this.parseTenantId(this._activatedRoute.snapshot.queryParams['tenantId'])
            );
        }
    }

    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnRegister();
    }

    save(): void {
        this.saving.set(true);
        this._accountService
            .resetPassword(this.model)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe((result: ResetPasswordOutput) => {
                if (!result.canLogin) {
                    this._router.navigate(['account/login']);
                    return;
                }

                let recaptchaCallback = (token: string) => {
                    // Autheticate
                    this.saving.set(true);
                    this._loginService.authenticateModel.userNameOrEmailAddress = result.userName;
                    this._loginService.authenticateModel.password = this.model.password;
                    this._loginService.authenticate(
                        () => {
                            this.saving.set(false);
                        },
                        null,
                        token
                    );
                };

                if (this._recaptchaWrapperService.useCaptchaOnResetPassword()) {
                    this._recaptchaWrapperService
                        .getService()
                        .execute('login')
                        .subscribe((token) => recaptchaCallback(token));
                } else {
                    recaptchaCallback(null);
                }
            });
    }

    parseTenantId(tenantIdAsStr?: string): number {
        let tenantId = !tenantIdAsStr ? undefined : parseInt(tenantIdAsStr, 10);
        if (Number.isNaN(tenantId)) {
            tenantId = undefined;
        }

        return tenantId;
    }
}
