import { Component, ElementRef, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    SubscribableEditionComboboxItemDto,
    TenantEditDto,
    TenantServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { filter as _filter } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize, switchMap } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { DatePickerModule } from 'primeng/datepicker';
import { PrimengDatePickerLuxonModifierDirective } from '../../../shared/utils/date-time/primeng-date-picker-luxon-modifier.directive';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'editTenantModal',
    templateUrl: './edit-tenant-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        DatePickerModule,
        PrimengDatePickerLuxonModifierDirective,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class EditTenantModalComponent extends AppComponentBase {
    private _tenantService = inject(TenantServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);

    readonly nameInput = viewChild<ElementRef>('nameInput');
    readonly modal = viewChild<ModalDirective>('editModal');
    readonly subscriptionEndDateUtc = viewChild<ElementRef>('SubscriptionEndDateUtc');

    readonly modalSave = output<any>();

    active = signal(false);
    saving = signal(false);
    isUnlimited = false;
    subscriptionEndDateUtcIsValid = false;

    tenant = signal<TenantEditDto>(undefined);
    currentConnectionString: string;
    editions = signal<SubscribableEditionComboboxItemDto[]>([]);
    isSubscriptionFieldsVisible = false;

    show(tenantId: number): void {
        this.active.set(true);

        this._commonLookupService
            .getEditionsForCombobox(false)
            .pipe(
                switchMap((editionsResult) => {
                    const editionsList = [...editionsResult.items];
                    let notSelectedEdition = new SubscribableEditionComboboxItemDto();
                    notSelectedEdition.displayText = this.l('NotAssigned');
                    notSelectedEdition.value = '';
                    editionsList.unshift(notSelectedEdition);
                    this.editions.set(editionsList);
                    return this._tenantService.getTenantForEdit(tenantId);
                })
            )
            .subscribe((tenantResult) => {
                this.currentConnectionString = tenantResult.connectionString;
                tenantResult.editionId = tenantResult.editionId || 0;
                this.isUnlimited = !tenantResult.subscriptionEndDateUtc;
                this.subscriptionEndDateUtcIsValid =
                    this.isUnlimited || tenantResult.subscriptionEndDateUtc !== undefined;
                this.tenant.set(tenantResult);
                this.toggleSubscriptionFields();
                this.modal().show();
            });
    }

    onShown(): void {
        const nameInputEl = this.nameInput();
        if (nameInputEl?.nativeElement) {
            nameInputEl.nativeElement.focus();
        }
    }

    subscriptionEndDateChange(date: DateTime | null): void {
        this.subscriptionEndDateUtcIsValid = date !== null || this.isUnlimited;
        this.updateTenantProperty('subscriptionEndDateUtc', date);
    }

    selectedEditionIsFree(): boolean {
        const tenantValue = this.tenant();
        if (!tenantValue?.editionId) {
            return true;
        }

        let selectedEditions = _filter(this.editions(), { value: tenantValue.editionId + '' });
        if (selectedEditions.length !== 1) {
            return true;
        }

        let selectedEdition = selectedEditions[0];
        return selectedEdition.isFree;
    }

    save(): void {
        this.saving.set(true);
        const tenantValue = this.tenant();
        if (tenantValue.editionId === 0) {
            tenantValue.editionId = null;
        }

        if (this.isUnlimited) {
            tenantValue.isInTrialPeriod = false;
        }

        if (this.isUnlimited || !tenantValue.editionId) {
            tenantValue.subscriptionEndDateUtc = null;
        } else if (tenantValue.subscriptionEndDateUtc) {
            tenantValue.subscriptionEndDateUtc = tenantValue.subscriptionEndDateUtc.toLocal();
        }

        this._tenantService
            .updateTenant(tenantValue)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    onEditionChange(): void {
        const tenantValue = this.tenant();
        if (this.selectedEditionIsFree()) {
            tenantValue.isInTrialPeriod = false;
        }

        this.toggleSubscriptionFields();
    }

    onUnlimitedChange(): void {
        const tenantValue = this.tenant();
        if (this.isUnlimited) {
            tenantValue.subscriptionEndDateUtc = null;
            this.subscriptionEndDateUtcIsValid = true;
            tenantValue.isInTrialPeriod = false;
        } else {
            if (!tenantValue.subscriptionEndDateUtc) {
                this.subscriptionEndDateUtcIsValid = false;
            }
        }
    }

    toggleSubscriptionFields() {
        const tenantValue = this.tenant();
        if (tenantValue?.editionId > 0) {
            this.isSubscriptionFieldsVisible = true;
        } else {
            this.isSubscriptionFieldsVisible = false;
        }
    }

    updateTenantProperty<K extends keyof TenantEditDto>(key: K, value: TenantEditDto[K]): void {
        const current = this.tenant();
        if (current) {
            (current as any)[key] = value;
            this.tenant.set(current);
        }
    }
}
