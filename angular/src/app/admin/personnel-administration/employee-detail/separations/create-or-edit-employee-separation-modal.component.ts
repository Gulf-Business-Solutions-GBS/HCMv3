import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSeparationDto,
    EmployeeSeparationsServiceProxy,
    GetAllSeparationReasonsInput,
    SeparationReasonDto,
    SeparationReasonsServiceProxy,
    SeparationType,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeSeparationModal',
    templateUrl: './create-or-edit-employee-separation-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSeparationModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSeparationsServiceProxy);
    private _separationReasonsService = inject(SeparationReasonsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSeparationDto>(new CreateOrEditEmployeeSeparationDto());
    readonly separationReasons = signal<SeparationReasonDto[]>([]);
    readonly separationTypes = [
        { value: SeparationType.Resignation, name: this.l('Resignation') },
        { value: SeparationType.Termination, name: this.l('Termination') },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._separationReasonsService
            .getAll(new GetAllSeparationReasonsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.separationReasons.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeSeparationDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSeparationForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSeparation);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSeparation_SubmissionDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSeparationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSeparationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateSubmissionDate(value: string): void {
        this.updateField('submissionDate', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateLastDayInService(value: string): void {
        this.updateField('lastDayInService', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updatePlannedLastDayInService(value: string): void {
        this.updateField('plannedLastDayInService', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateRevokeDate(value: string): void {
        this.updateField('revokeDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
