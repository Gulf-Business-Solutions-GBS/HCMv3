import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditCVQualificationDto,
    CVQualificationsServiceProxy,
    GetAllSimpleLookupsInput,
    QualificationsServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCvQualificationModal',
    templateUrl: './create-or-edit-cv-qualification-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCvQualificationModalComponent extends AppComponentBase {
    private _service = inject(CVQualificationsServiceProxy);
    private _qualificationsService = inject(QualificationsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditCVQualificationDto>(new CreateOrEditCVQualificationDto());

    readonly qualifications = signal<SimpleLookupDto[]>([]);

    show(applicantCVId: number, id?: number): void {
        this.active.set(true);

        this._qualificationsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.qualifications.set(result.items));

        if (!id) {
            const record = new CreateOrEditCVQualificationDto();
            record.applicantCVId = applicantCVId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getCVQualificationForEdit(id).subscribe((result) => {
                this.record.set(result.cvQualification);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CVQualification_QualificationId')?.focus();
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

    updateField(field: keyof CreateOrEditCVQualificationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditCVQualificationDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
