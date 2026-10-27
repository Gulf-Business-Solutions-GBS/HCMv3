import { AfterViewInit, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EditionSelectDto,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    RegisterTenantOutput,
    TenantRegistrationServiceProxy,
    PaymentPeriodType,
    SubscriptionPaymentGatewayType,
    SubscriptionStartType,
} from '@shared/service-proxies/service-proxies';
import { RegisterTenantModel } from './register-tenant.model';
import { TenantRegistrationHelperService } from './tenant-registration-helper.service';
import { finalize } from 'rxjs/operators';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { ReCaptchaV3Service, RecaptchaLoaderService, RECAPTCHA_V3_SITE_KEY } from 'ng-recaptcha-2';
import { AppConsts } from '@shared/AppConsts';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { EqualValidator } from '../../shared/utils/validation/equal-validator.directive';
import { PasswordModule } from 'primeng/password';
import { PasswordComplexityValidator } from '../../shared/utils/validation/password-complexity-validator.directive';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './register-tenant.component.html',
    animations: [accountModuleAnimation()],
    providers: [
        ReCaptchaV3WrapperService,
        ReCaptchaV3Service,
        RecaptchaLoaderService,
        { provide: RECAPTCHA_V3_SITE_KEY, useValue: AppConsts.recaptchaSiteKey },
    ],
    imports: [
        RouterLink,
        FormsModule,
        AutoFocusDirective,
        ValidationMessagesComponent,
        NgClass,
        EqualValidator,
        PasswordModule,
        PasswordComplexityValidator,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class RegisterTenantComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _tenantRegistrationService = inject(TenantRegistrationServiceProxy);
    private _router = inject(Router);
    private _profileService = inject(ProfileServiceProxy);
    private _tenantRegistrationHelper = inject(TenantRegistrationHelperService);
    private _activatedRoute = inject(ActivatedRoute);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);

    model: RegisterTenantModel = new RegisterTenantModel();
    readonly passwordComplexitySetting = signal<PasswordComplexitySetting>(new PasswordComplexitySetting());
    subscriptionStartType = SubscriptionStartType;
    paymentPeriodType = PaymentPeriodType;
    selectedPaymentPeriodType: PaymentPeriodType = PaymentPeriodType.Monthly;
    subscriptionPaymentGateway = SubscriptionPaymentGatewayType;
    paymentId = '';

    readonly saving = signal(false);

    ngOnInit() {
        this.model.editionId = this._activatedRoute.snapshot.queryParams['editionId'];
        this.model.paymentPeriodType = this._activatedRoute.snapshot.queryParams['paymentPeriodType'];
        if (this.model.editionId) {
            this.model.subscriptionStartType = this._activatedRoute.snapshot.queryParams['subscriptionStartType'];
        }

        //Prevent to create tenant in a tenant context
        if (this.appSession.tenant != null) {
            this._router.navigate(['account/login']);
            return;
        }

        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting.set(result.setting);
        });
    }

    ngAfterViewInit() {
        // After the page is fully loaded, scrolls the window to the top
        window.scrollTo(0, 0);

        if (this.model.editionId) {
            this._tenantRegistrationService.getEdition(this.model.editionId).subscribe((result: EditionSelectDto) => {
                this.model.edition = result;
            });
        }

        this._recaptchaWrapperService.setCaptchaVisibilityOnRegister();
    }

    save(): void {
        let recaptchaCallback = (token: string) => {
            this.saving.set(true);
            this.model.captchaResponse = token;
            this.model.successUrl = abp.appPath + 'account/buy-succeed';
            this.model.errorUrl = abp.appPath + 'account/payment-failed';
            this._tenantRegistrationService
                .registerTenant(this.model)
                .pipe(
                    finalize(() => {
                        this.saving.set(false);
                    })
                )
                .subscribe((result: RegisterTenantOutput) => {
                    this.notify.success(this.l('SuccessfullyRegistered'));
                    this._tenantRegistrationHelper.registrationResult = result;

                    if (parseInt(this.model.subscriptionStartType.toString()) === SubscriptionStartType.Paid) {
                        this._router.navigate(['account/gateway-selection'], {
                            queryParams: {
                                paymentId: result.paymentId,
                            },
                        });
                    } else {
                        this._router.navigate(['account/register-tenant-result']);
                    }
                });
        };

        if (this._recaptchaWrapperService.useCaptchaOnRegister()) {
            this._recaptchaWrapperService
                .getService()
                .execute('register_tenant')
                .subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
}
