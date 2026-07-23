import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeTimePolicyDto,
    EmployeeTimePoliciesServiceProxy,
    GetAllTimePoliciesInput,
    TimePoliciesServiceProxy,
    TimePolicyDto,
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
    selector: 'createOrEditEmployeeTimePolicyModal',
    templateUrl: './create-or-edit-employee-time-policy-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeTimePolicyModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTimePoliciesServiceProxy);
    private _timePoliciesService = inject(TimePoliciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTimePolicyDto>(new CreateOrEditEmployeeTimePolicyDto());

    readonly timePolicies = signal<TimePolicyDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeTimePolicyDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTimePolicyForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTimePolicy);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._timePoliciesService
            .getAll(new GetAllTimePoliciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.timePolicies.set(result.items));
    }

    onShown(): void {
        document.getElementById('EmployeeTimePolicy_StartDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeTimePolicyDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeTimePolicyDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
