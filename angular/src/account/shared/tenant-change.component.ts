import { Component, OnInit, inject, viewChild, signal, computed } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountServiceProxy } from '@shared/service-proxies/service-proxies';
import { TenantChangeModalComponent } from './tenant-change-modal.component';

import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'tenant-change',
    template: `
        @if (isMultiTenancyEnabled()) {
            <span>
                {{ 'CurrentTenant' | localize }}:
                @if (tenancyName()) {
                    <span title="{{ name() }}">
                        <strong>{{ tenancyName() }}</strong>
                    </span>
                }
                @if (!tenancyName()) {
                    <span>{{ 'NotSelected' | localize }}</span>
                }
                (
                <a href="javascript:;" (click)="showChangeModal()">{{ l('Change') }}</a>
                )
                <tenantChangeModal #tenantChangeModal />
            </span>
        }
    `,
    imports: [TenantChangeModalComponent, LocalizePipe],
})
export class TenantChangeComponent extends AppComponentBase implements OnInit {
    private _accountService = inject(AccountServiceProxy);

    readonly tenantChangeModal = viewChild<TenantChangeModalComponent>('tenantChangeModal');

    tenancyName = signal<string>('');
    name = signal<string>('');

    isMultiTenancyEnabled = computed(() => abp.multiTenancy.isEnabled);

    ngOnInit() {
        if (this.appSession.tenant) {
            this.tenancyName.set(this.appSession.tenant.tenancyName);
            this.name.set(this.appSession.tenant.name);
        }
    }

    showChangeModal(): void {
        this.tenantChangeModal().show(this.tenancyName());
    }
}
