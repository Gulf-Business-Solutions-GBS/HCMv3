import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeDependantDto,
    DependantTypeDto,
    DependantTypesServiceProxy,
    EmployeeDependantsServiceProxy,
    GetAllDependantTypesInput,
    GetAllTicketTypesInput,
    TicketTypeDto,
    TicketTypesServiceProxy,
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
    selector: 'createOrEditEmployeeDependantModal',
    templateUrl: './create-or-edit-employee-dependant-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeDependantModalComponent extends AppComponentBase {
    private _service = inject(EmployeeDependantsServiceProxy);
    private _dependantTypesService = inject(DependantTypesServiceProxy);
    private _ticketTypesService = inject(TicketTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeDependantDto>(new CreateOrEditEmployeeDependantDto());

    readonly dependantTypes = signal<DependantTypeDto[]>([]);
    readonly ticketTypes = signal<TicketTypeDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._dependantTypesService
            .getAll(
                new GetAllDependantTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.dependantTypes.set(result.items));

        this._ticketTypesService
            .getAll(
                new GetAllTicketTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.ticketTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeDependantDto();
            record.employeeId = employeeId;
            record.specialNeed = false;
            record.includeInSocialSecurity = false;
            record.includeInHealthInsurance = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeDependantForEdit(id).subscribe((result) => {
                this.record.set(result.employeeDependant);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeDependant_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeDependantDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDependantDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeDependantDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
