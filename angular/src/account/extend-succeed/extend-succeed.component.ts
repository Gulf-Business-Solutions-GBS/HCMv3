import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TenantRegistrationServiceProxy } from '@shared/service-proxies/service-proxies';

@Component({
    selector: 'extend-succeed',
    templateUrl: './extend-succeed.component.html',
})
export class ExtendSucceedComponent extends AppComponentBase implements OnInit {
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    private _tenantRegistrationService = inject(TenantRegistrationServiceProxy);

    ngOnInit(): void {
        let paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        this._tenantRegistrationService.extendSucceed(paymentId).subscribe(() => {
            this._router.navigate([abp.appPath + 'app/admin/subscription-management']);
        });
    }
}
