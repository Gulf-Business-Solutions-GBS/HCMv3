import { AfterViewInit, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountServiceProxy, SendEmailActivationLinkInput } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ReCaptchaV3Service, RecaptchaLoaderService, RECAPTCHA_V3_SITE_KEY } from 'ng-recaptcha-2';
import { AppConsts } from '@shared/AppConsts';

@Component({
    templateUrl: './email-activation.component.html',
    animations: [accountModuleAnimation()],
    imports: [FormsModule, ValidationMessagesComponent, RouterLink, ButtonBusyDirective, LocalizePipe],
    providers: [
        ReCaptchaV3WrapperService,
        ReCaptchaV3Service,
        RecaptchaLoaderService,
        { provide: RECAPTCHA_V3_SITE_KEY, useValue: AppConsts.recaptchaSiteKey },
    ],
})
export class EmailActivationComponent extends AppComponentBase implements AfterViewInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);

    model: SendEmailActivationLinkInput = new SendEmailActivationLinkInput();
    readonly saving = signal<boolean>(false);

    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnEmailActivation();
    }

    save(): void {
        let recaptchaCallback = (token: string) => {
            this.saving.set(true);
            this.model.captchaResponse = token;
            this._accountService
                .sendEmailActivationLink(this.model)
                .pipe(
                    finalize(() => {
                        this.saving.set(false);
                    })
                )
                .subscribe(() => {
                    this.message
                        .success(this.l('ActivationMailSentIfEmailAssociatedMessage'), this.l('MailSent'))
                        .then(() => {
                            this._router.navigate(['account/login']);
                        });
                });
        };

        if (this._recaptchaWrapperService.useCaptchaOnEmailActivation()) {
            this._recaptchaWrapperService
                .getService()
                .execute('emailActivation')
                .subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
}
