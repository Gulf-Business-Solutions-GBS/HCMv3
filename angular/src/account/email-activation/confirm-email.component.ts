import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppAuthService } from '@app/shared/common/auth/app-auth.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountServiceProxy, ActivateEmailInput, ResolveTenantIdInput } from '@shared/service-proxies/service-proxies';

@Component({
    template: `
        <div class="login-form">
            <div class="alert alert-success text-center" role="alert">
                <div class="alert-text">{{ waitMessage() }}</div>
            </div>
        </div>
    `,
})
export class ConfirmEmailComponent extends AppComponentBase implements OnInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    private _appAuthService = inject(AppAuthService);

    readonly waitMessage = signal('');

    model: ActivateEmailInput = new ActivateEmailInput();

    ngOnInit(): void {
        this.waitMessage.set(this.l('PleaseWaitToConfirmYourEmailMessage'));
        this.model.c = this._activatedRoute.snapshot.queryParams['c'];

        this._accountService.resolveTenantId(new ResolveTenantIdInput({ c: this.model.c })).subscribe((tenantId) => {
            if (this.appSession.user && this.appSession.tenantId !== tenantId) {
                this._appAuthService.logout(true, window.location.href);
                return;
            }

            abp.multiTenancy.setTenantIdCookie(tenantId);

            this._accountService.activateEmail(this.model).subscribe(() => {
                this.notify.success(this.l('YourEmailIsConfirmedMessage'), '', {
                    willClose: () => {
                        this._router.navigate(['account/login']);
                    },
                });
            });
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
