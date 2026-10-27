import { Injectable, inject } from '@angular/core';
import { AppAuthService } from '@app/shared/common/auth/app-auth.service';
import { AppUrlService } from '@shared/common/nav/app-url.service';
import {
    AccountServiceProxy,
    SwitchToLinkedAccountInput,
    SwitchToLinkedAccountOutput,
} from '@shared/service-proxies/service-proxies';

@Injectable()
export class LinkedAccountService {
    private _accountService = inject(AccountServiceProxy);
    private _appUrlService = inject(AppUrlService);
    private _authService = inject(AppAuthService);

    switchToAccount(userId: number, tenantId?: number): void {
        const input = new SwitchToLinkedAccountInput();
        input.targetUserId = userId;
        input.targetTenantId = tenantId;

        this._accountService.switchToLinkedAccount(input).subscribe((result: SwitchToLinkedAccountOutput) => {
            let targetUrl =
                this._appUrlService.getAppRootUrlOfTenant(result.tenancyName) +
                '?switchAccountToken=' +
                result.switchAccountToken;
            if (input.targetTenantId) {
                targetUrl = targetUrl + '&tenantId=' + input.targetTenantId;
            }

            this._authService.logout(true, targetUrl);
        });
    }
}
