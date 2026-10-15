import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeManagerDto,
    EmployeeDto,
    EmployeeManagersServiceProxy,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeManagerModal',
    templateUrl: './create-or-edit-employee-manager-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeManagerModalComponent extends AppComponentBase {
    private _service = inject(EmployeeManagersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeManagerDto>(new CreateOrEditEmployeeManagerDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeManager',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly employees = signal<EmployeeDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items.filter((e) => e.id !== employeeId)));

        if (!id) {
            const record = new CreateOrEditEmployeeManagerDto();
            record.employeeId = employeeId;
            // Today by default, as on every effective-dated record: most changes take effect now.
            record.startDate = DateTime.now().startOf('day');
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeManagerForEdit(id).subscribe((result) => {
                this.record.set(result.employeeManager);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeManager_StartDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeManagerDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeManagerDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
