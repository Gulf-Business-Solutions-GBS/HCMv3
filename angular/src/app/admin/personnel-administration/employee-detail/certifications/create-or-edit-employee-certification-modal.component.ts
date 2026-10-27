import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CertificationType,
    CreateOrEditEmployeeCertificationDto,
    EmployeeCertificationsServiceProxy,
    GetAllInstitutionsInput,
    InstitutionDto,
    InstitutionsServiceProxy,
} from '@shared/service-proxies/service-proxies';
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
    selector: 'createOrEditEmployeeCertificationModal',
    templateUrl: './create-or-edit-employee-certification-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeCertificationModalComponent extends AppComponentBase {
    private _service = inject(EmployeeCertificationsServiceProxy);
    private _institutionsService = inject(InstitutionsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeCertificationDto>(new CreateOrEditEmployeeCertificationDto());
    readonly institutions = signal<InstitutionDto[]>([]);

    readonly certificationTypes = [
        { value: CertificationType.Professional, name: 'Professional' },
        { value: CertificationType.Training, name: 'Training' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._institutionsService
            .getAll(
                new GetAllInstitutionsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.institutions.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeCertificationDto();
            record.employeeId = employeeId;
            record.certificationType = CertificationType.Professional;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeCertificationForEdit(id).subscribe((result) => {
                this.record.set(result.employeeCertification);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeCertification_Course')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeCertificationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeCertificationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'startDate' | 'endDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
