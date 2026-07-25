import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EducationCertificatesServiceProxy,
    CreateOrEditJobDescriptionCertificateDto,
    JobDescriptionCertificatesServiceProxy,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditJobDescriptionCertificateModal',
    templateUrl: './create-or-edit-job-description-certificate-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditJobDescriptionCertificateModalComponent extends AppComponentBase {
    private _service = inject(JobDescriptionCertificatesServiceProxy);
    private _educationCertificatesService = inject(EducationCertificatesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditJobDescriptionCertificateDto>(new CreateOrEditJobDescriptionCertificateDto());
    readonly educationCertificates = signal<SimpleLookupDto[]>([]);

    show(jobDescriptionId: number, id?: number): void {
        this.active.set(true);

        this._educationCertificatesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.educationCertificates.set(result.items));

        if (!id) {
            const record = new CreateOrEditJobDescriptionCertificateDto();
            record.jobDescriptionId = jobDescriptionId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getJobDescriptionCertificateForEdit(id).subscribe((result) => {
                this.record.set(result.jobDescriptionCertificate);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('JobDescriptionCertificate_EducationCertificateId')?.focus();
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

    updateField(field: keyof CreateOrEditJobDescriptionCertificateDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditJobDescriptionCertificateDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
