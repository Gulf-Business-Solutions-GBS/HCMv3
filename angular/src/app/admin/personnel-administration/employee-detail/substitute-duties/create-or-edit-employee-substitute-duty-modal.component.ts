import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSubstituteDutyDto,
    DutiesServiceProxy,
    DutyDto,
    EmployeeSubstituteDutiesServiceProxy,
    GetAllDutiesInput,
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
    selector: 'createOrEditEmployeeSubstituteDutyModal',
    templateUrl: './create-or-edit-employee-substitute-duty-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeSubstituteDutyModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubstituteDutiesServiceProxy);
    private _dutiesService = inject(DutiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubstituteDutyDto>(new CreateOrEditEmployeeSubstituteDutyDto());

    readonly duties = signal<DutyDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeSubstituteDutyDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubstituteDutyForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubstituteDuty);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._dutiesService
            .getAll(new GetAllDutiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.duties.set(result.items));
    }

    onShown(): void {
        document.getElementById('EmployeeSubstituteDuty_SubstituteDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubstituteDutyDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubstituteDutyDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateSubstituteDate(value: string): void {
        this.updateField('substituteDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
