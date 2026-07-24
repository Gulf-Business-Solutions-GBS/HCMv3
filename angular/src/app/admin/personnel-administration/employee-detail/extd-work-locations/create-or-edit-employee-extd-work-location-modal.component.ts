import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeExtdWorkLocationDto,
    EmployeeExtdWorkLocationsServiceProxy,
    GetAllWorkLocationsInput,
    WorkLocationDto,
    WorkLocationsServiceProxy,
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
    selector: 'createOrEditEmployeeExtdWorkLocationModal',
    templateUrl: './create-or-edit-employee-extd-work-location-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeExtdWorkLocationModalComponent extends AppComponentBase {
    private _service = inject(EmployeeExtdWorkLocationsServiceProxy);
    private _workLocationsService = inject(WorkLocationsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeExtdWorkLocationDto>(new CreateOrEditEmployeeExtdWorkLocationDto());

    readonly workLocations = signal<WorkLocationDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._workLocationsService
            .getAll(new GetAllWorkLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workLocations.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeExtdWorkLocationDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeExtdWorkLocationForEdit(id).subscribe((result) => {
                this.record.set(result.employeeExtdWorkLocation);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeExtdWorkLocation_WorkLocationId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeExtdWorkLocationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeExtdWorkLocationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeExtdWorkLocationDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
