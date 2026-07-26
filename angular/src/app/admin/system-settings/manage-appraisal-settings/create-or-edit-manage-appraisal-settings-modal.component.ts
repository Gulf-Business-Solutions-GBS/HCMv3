import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppraisalSettingsEditDto, ManageAppraisalSettingsServiceProxy, AppraisalCycle, MarksOn } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageAppraisalSettingsModal',
    templateUrl: './create-or-edit-manage-appraisal-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageAppraisalSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageAppraisalSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<AppraisalSettingsEditDto>(new AppraisalSettingsEditDto());

    readonly appraisalCycles = [
        { value: AppraisalCycle.Quarterly, name: 'Quarterly' },
        { value: AppraisalCycle.HalfYearly, name: 'HalfYearly' },
        { value: AppraisalCycle.Annually, name: 'Annually' },
    ];

    readonly marksOnOptions = [
        { value: MarksOn.Ratings, name: 'Ratings' },
        { value: MarksOn.Weight, name: 'Weight' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new AppraisalSettingsEditDto();
            record.appraisalCycle = AppraisalCycle.Quarterly;
            record.enablePAPeerReview = false;
            record.setPeerMinLimit = 0;
            record.setPeerMaxLimit = 0;
            record.enableMarksInPAReview = false;
            record.marksOn = MarksOn.Ratings;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getAppraisalSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AppraisalSettings_AppraisalCycle')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditAppraisalSettings(this.record())
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

    updateField(field: keyof AppraisalSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new AppraisalSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof AppraisalSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
