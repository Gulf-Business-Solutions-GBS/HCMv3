import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeRelationshipDto,
    EmployeeDto,
    EmployeeRelationshipsServiceProxy,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    RelationshipTypesServiceProxy,
    SimpleLookupDto,
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
    selector: 'createOrEditEmployeeRelationshipModal',
    templateUrl: './create-or-edit-employee-relationship-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeRelationshipModalComponent extends AppComponentBase {
    private _service = inject(EmployeeRelationshipsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _relationshipTypesService = inject(RelationshipTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeRelationshipDto>(new CreateOrEditEmployeeRelationshipDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly relationshipTypes = signal<SimpleLookupDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items.filter((e) => e.id !== employeeId)));

        this._relationshipTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.relationshipTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeRelationshipDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeRelationshipForEdit(id).subscribe((result) => {
                this.record.set(result.employeeRelationship);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeRelationship_RelationshipTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeRelationshipDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeRelationshipDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'startDate' | 'endDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
