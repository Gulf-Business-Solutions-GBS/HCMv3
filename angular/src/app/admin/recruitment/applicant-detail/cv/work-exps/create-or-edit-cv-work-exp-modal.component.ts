import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditCVWorkExpDto, CVWorkExpsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditCvWorkExpModal',
    templateUrl: './create-or-edit-cv-work-exp-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditCvWorkExpModalComponent extends AppComponentBase {
    private _service = inject(CVWorkExpsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditCVWorkExpDto>(new CreateOrEditCVWorkExpDto());

    show(applicantCVId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditCVWorkExpDto();
            record.applicantCVId = applicantCVId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getCVWorkExpForEdit(id).subscribe((result) => {
                this.record.set(result.cvWorkExp);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CVWorkExp_Company')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditCVWorkExpDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditCVWorkExpDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'startDate' | 'endDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
