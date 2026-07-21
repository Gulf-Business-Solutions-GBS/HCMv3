import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountServiceProxy,
    IsTenantAvailableInput,
    IsTenantAvailableOutput,
    TenantAvailabilityState,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'tenantChangeModal',
    templateUrl: './tenant-change-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, NgClass, ButtonBusyDirective, LocalizePipe],
})
export class TenantChangeModalComponent extends AppComponentBase {
    private _accountService = inject(AccountServiceProxy);

    readonly modal = viewChild<ModalDirective>('tenantChangeModal');
    readonly tenancyNameInput = viewChild<ElementRef>('tenancyNameInput');

    tenancyName = signal('');
    tenancyNameBuffer = signal('');
    submitButtonText = signal('');
    isSwitchToTenant = signal(false);
    active = signal(false);
    saving = signal(false);

    focusTenancyNameInput(): void {
        const input = this.tenancyNameInput();
        if (input?.nativeElement) {
            input.nativeElement.focus();
        }
    }

    updateSubmitButtonText(): void {
        if (this.isSwitchToTenant()) {
            this.submitButtonText.set(this.l('SwitchToTheTenant'));
        } else {
            this.submitButtonText.set(this.l('SwitchToTheHost'));
        }
    }

    show(tenancyName: string): void {
        this.tenancyName.set(tenancyName);
        this.isSwitchToTenant.set(tenancyName ? true : false);
        this.updateSubmitButtonText();
        this.active.set(true);
        this.modal().show();
    }

    onShown(): void {
        this.focusTenancyNameInput();
    }

    switchToTenant(e): void {
        if (e.target.checked) {
            this.tenancyName.set(this.tenancyNameBuffer());
            this.focusTenancyNameInput();
        } else {
            this.tenancyNameBuffer.set(this.tenancyName());
            this.tenancyName.set('');
        }

        this.updateSubmitButtonText();
    }

    save(): void {
        if (!this.tenancyName()) {
            abp.multiTenancy.setTenantIdCookie(undefined);
            this.close();
            location.reload();
            return;
        }

        let input = new IsTenantAvailableInput();
        input.tenancyName = this.tenancyName();

        this.saving.set(true);
        this._accountService
            .isTenantAvailable(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result: IsTenantAvailableOutput) => {
                switch (result.state) {
                    case TenantAvailabilityState.Available:
                        abp.multiTenancy.setTenantIdCookie(result.tenantId);
                        this.close();
                        location.reload();
                        return;
                    case TenantAvailabilityState.InActive:
                        this.message.warn(this.l('TenantIsNotActive', this.tenancyName()));
                        this.focusTenancyNameInput();
                        break;
                    case TenantAvailabilityState.NotFound: //NotFound
                        this.message.warn(this.l('ThereIsNoTenantDefinedWithName{0}', this.tenancyName()));
                        this.focusTenancyNameInput();
                        break;
                }
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
