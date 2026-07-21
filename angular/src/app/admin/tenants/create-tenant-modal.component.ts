import { Component, inject, output, viewChild, signal } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    CreateTenantInput,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    TenantServiceProxy,
    SubscribableEditionComboboxItemDto,
} from '@shared/service-proxies/service-proxies';
import { filter as _filter } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { EqualValidator } from '../../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../../shared/utils/validation/password-complexity-validator.directive';
import { DatePickerModule } from 'primeng/datepicker';
import { PrimengDatePickerLuxonModifierDirective } from '../../../shared/utils/date-time/primeng-date-picker-luxon-modifier.directive';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createTenantModal',
    templateUrl: './create-tenant-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        EqualValidator,
        PasswordComplexityValidator,
        DatePickerModule,
        PrimengDatePickerLuxonModifierDirective,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateTenantModalComponent extends AppComponentBase {
    private _tenantService = inject(TenantServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    private _profileService = inject(ProfileServiceProxy);
    private _dateTimeService = inject(DateTimeService);

    readonly modal = viewChild<ModalDirective>('createModal');

    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    setRandomPassword = true;
    useHostDb = true;
    editions = signal<SubscribableEditionComboboxItemDto[]>([]);
    tenant: CreateTenantInput;
    passwordComplexitySetting: PasswordComplexitySetting = new PasswordComplexitySetting();
    isUnlimited = false;
    isSubscriptionFieldsVisible = false;
    isSelectedEditionFree = false;
    tenantAdminPasswordRepeat = '';

    show() {
        this.active.set(true);
        this.init();

        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting = result.setting;
            this.modal().show();
        });
    }

    onShown(): void {
        document.getElementById('TenancyName').focus();
    }

    init(): void {
        this.tenant = new CreateTenantInput();
        this.tenant.isActive = true;
        this.tenant.shouldChangePasswordOnNextLogin = true;
        this.tenant.sendActivationEmail = true;
        this.tenant.editionId = 0;
        this.tenant.isInTrialPeriod = false;

        this._commonLookupService.getEditionsForCombobox(false).subscribe((result) => {
            const editionsList = [...result.items];

            let notAssignedItem = new SubscribableEditionComboboxItemDto();
            notAssignedItem.value = '';
            notAssignedItem.displayText = this.l('NotAssigned');

            editionsList.unshift(notAssignedItem);
            this.editions.set(editionsList);

            this._commonLookupService.getDefaultEditionName().subscribe((getDefaultEditionResult) => {
                const currentEditions = this.editions();
                let defaultEdition = _filter(currentEditions, { displayText: getDefaultEditionResult.name });
                if (defaultEdition && defaultEdition[0]) {
                    this.tenant.editionId = parseInt(defaultEdition[0].value);
                    this.toggleSubscriptionFields();
                }
            });
        });
    }

    getEditionValue(item): number {
        return parseInt(item.value);
    }

    selectedEditionIsFree(): boolean {
        const currentEditions = this.editions();
        let selectedEditions = _filter(currentEditions, { value: this.tenant.editionId.toString() }).map((u) =>
            Object.assign(new SubscribableEditionComboboxItemDto(), u)
        );

        if (selectedEditions.length !== 1) {
            this.isSelectedEditionFree = true;
        }

        let selectedEdition = selectedEditions[0];
        this.isSelectedEditionFree = selectedEdition.isFree;
        return this.isSelectedEditionFree;
    }

    subscriptionEndDateIsValid(): boolean {
        if (this.tenant.editionId <= 0) {
            return true;
        }

        if (this.isUnlimited) {
            return true;
        }

        if (!this.tenant.subscriptionEndDateUtc) {
            return false;
        }

        return this.tenant.subscriptionEndDateUtc !== undefined;
    }

    save(): void {
        this.saving.set(true);

        if (this.setRandomPassword) {
            this.tenant.adminPassword = null;
        }

        if (this.tenant.editionId === 0) {
            this.tenant.editionId = null;
        }

        if (this.isUnlimited) {
            this.tenant.isInTrialPeriod = false;
        }

        if (this.isUnlimited || this.tenant.editionId <= 0) {
            this.tenant.subscriptionEndDateUtc = null;
        }

        this._tenantService
            .createTenant(this.tenant)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    close(): void {
        this.active.set(false);
        this.tenantAdminPasswordRepeat = '';
        this.editions.set([]);
        this.modal().hide();
    }

    onEditionChange(): void {
        this.tenant.isInTrialPeriod = this.tenant.editionId > 0 && !this.selectedEditionIsFree();
        this.toggleSubscriptionFields();
    }

    toggleSubscriptionFields() {
        this.isSelectedEditionFree = this.selectedEditionIsFree();
        if (this.tenant.editionId <= 0 || this.isSelectedEditionFree) {
            this.isSubscriptionFieldsVisible = false;

            if (this.isSelectedEditionFree) {
                this.isUnlimited = true;
            } else {
                this.isUnlimited = false;
            }
        } else {
            this.isSubscriptionFieldsVisible = true;
        }
    }

    onIsUnlimitedChange() {
        if (this.isUnlimited) {
            this.tenant.isInTrialPeriod = false;
        }
    }
}
