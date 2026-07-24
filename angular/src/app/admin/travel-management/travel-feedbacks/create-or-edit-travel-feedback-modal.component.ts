import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTravelFeedbackDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllTravelsInput,
    TravelDto,
    TravelFeedbacksServiceProxy,
    TravelsServiceProxy,
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
    selector: 'createOrEditTravelFeedbackModal',
    templateUrl: './create-or-edit-travel-feedback-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTravelFeedbackModalComponent extends AppComponentBase {
    private _service = inject(TravelFeedbacksServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _travelsService = inject(TravelsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTravelFeedbackDto>(new CreateOrEditTravelFeedbackDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly travels = signal<TravelDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._travelsService
            .getAll(new GetAllTravelsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, employeeId: undefined, filter: undefined }))
            .subscribe((result) => this.travels.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditTravelFeedbackDto());
            this.modal().show();
        } else {
            this._service.getTravelFeedbackForEdit(id).subscribe((result) => {
                this.record.set(result.travelFeedback);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TravelFeedback_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
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

    updateField(field: keyof CreateOrEditTravelFeedbackDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTravelFeedbackDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditTravelFeedbackDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTravelFeedbackDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
