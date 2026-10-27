import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PersonnelSettingsEditDto,
    ManagePersonnelSettingsServiceProxy,
    ProbationDaysCalculation,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManagePersonnelSettingsModal',
    templateUrl: './create-or-edit-manage-personnel-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManagePersonnelSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManagePersonnelSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<PersonnelSettingsEditDto>(new PersonnelSettingsEditDto());

    readonly probationDaysCalculationOptions = [
        { value: ProbationDaysCalculation.CalendarDays, name: 'CalendarDays' },
        { value: ProbationDaysCalculation.WorkingDays, name: 'WorkingDays' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new PersonnelSettingsEditDto();
            record.enableSponsorGovtJobTitleQuota = false;
            record.maxPhotoSize = 0;
            record.probationDaysCalculation = ProbationDaysCalculation.CalendarDays;
            record.autoWageAssignOnHire = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPersonnelSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PersonnelSettings_EnableSponsorGovtJobTitleQuota')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditPersonnelSettings(this.record())
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

    updateField(field: keyof PersonnelSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new PersonnelSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof PersonnelSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
