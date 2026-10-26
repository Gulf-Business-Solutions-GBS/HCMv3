import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ManageGeneralSettingsEditDto,
    ManageGeneralSettingsServiceProxy,
    AutoFilterColumnCondition,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageGeneralSettingsModal',
    templateUrl: './create-or-edit-manage-general-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageGeneralSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageGeneralSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<ManageGeneralSettingsEditDto>(new ManageGeneralSettingsEditDto());

    readonly autoFilterConditions = [
        { value: AutoFilterColumnCondition.BeginsWith, name: 'BeginsWith' },
        { value: AutoFilterColumnCondition.Contains, name: 'Contains' },
        { value: AutoFilterColumnCondition.Equals, name: 'Equals' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new ManageGeneralSettingsEditDto();
            record.disableAutoEmpcode = false;
            record.dateEditMask = '';
            record.dateDisplayFormat = '';
            record.timeEditMask = '';
            record.timeDisplayFormat = '';
            record.startupDashboard = '';
            record.noticePeriodDaysExcludingSubmissionDate = false;
            record.enableGridDataAccessModeServer = false;
            record.startHangfire = false;
            record.autoFilterColumnCondition = AutoFilterColumnCondition.BeginsWith;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getGeneralSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('GeneralSettings_DateEditMask')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditGeneralSettings(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof ManageGeneralSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new ManageGeneralSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
